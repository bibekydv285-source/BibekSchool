using BibekSchool.Data;
using BibekSchool.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Threading;

namespace BibekSchool.Services
{
    public abstract class BaseService
    {
        protected readonly ApplicationDbContext _context;
        private static readonly ConcurrentQueue<AuditLog> _auditQueue = new();
        private static readonly SemaphoreSlim _auditSemaphore = new(1, 1);

        protected BaseService(ApplicationDbContext context)
        {
            _context = context;
        }

        protected async Task LogAuditAsync(string userId, string action, string entityType, string entityId, string? oldValues, string? newValues)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                CreatedAt = DateTime.UtcNow
            };

            _auditQueue.Enqueue(auditLog);

            // Flush synchronously if queue is getting large to avoid memory buildup
            if (_auditQueue.Count >= 10)
            {
                await FlushAuditQueueAsync();
            }
        }

        private async Task FlushAuditQueueAsync()
        {
            await _auditSemaphore.WaitAsync();
            try
            {
                if (_auditQueue.IsEmpty) return;

                var logs = new List<AuditLog>();
                while (_auditQueue.TryDequeue(out var log))
                {
                    logs.Add(log);
                }

                if (logs.Count > 0)
                {
                    _context.AuditLogs.AddRange(logs);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                // In production, consider persisting failed logs to a dead letter queue
            }
            finally
            {
                _auditSemaphore.Release();
            }
        }

        // Call this on application shutdown or when disposing
        public async Task FlushPendingAuditsAsync()
        {
            await FlushAuditQueueAsync();
        }
    }
}