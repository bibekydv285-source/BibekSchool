using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BibekSchool.Models;

namespace BibekSchool.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                logger.LogInformation("Starting database migration...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migration completed.");

                await SeedRolesAsync(roleManager, logger);
                await SeedMainAdminAsync(userManager, configuration, logger);
                await SeedDefaultAdminAsync(userManager, logger);
                await SeedInitialDataAsync(context, logger);
                logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                LogFullException(logger, ex, "Database seeding failed");
                throw;
            }
        }

        // Walks the InnerException chain and logs every level, so the real
        // root cause (constraint name, column, etc.) is visible in the logs
        // instead of just "See the inner exception for details."
        private static void LogFullException(ILogger logger, Exception ex, string context)
        {
            var level = 0;
            var current = ex;
            while (current != null)
            {
                logger.LogError(
                    "{Context} — [Level {Level}] {ExType}: {Message}",
                    context, level, current.GetType().Name, current.Message);
                current = current.InnerException;
                level++;
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            string[] roles = { "MainAdmin", "Admin", "Teacher", "Student" };

            foreach (var role in roles)
            {
                try
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        var result = await roleManager.CreateAsync(new IdentityRole(role));
                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                                logger.LogError("Role seeding error for {Role}: {Code} - {Description}",
                                    role, error.Code, error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogFullException(logger, ex, $"Failed seeding role '{role}'");
                    throw;
                }
            }
        }

        private static async Task SeedMainAdminAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            // Read from configuration instead of hardcoding — set via User Secrets (dev)
            // or Azure App Service Configuration (prod). Never committed to git.
            var mainAdminEmail = configuration["SeedAdmin:Email"];
            var mainAdminPassword = configuration["SeedAdmin:Password"];
            var mainAdminPhone = configuration["SeedAdmin:PhoneNumber"];
            var mainAdminFullName = configuration["SeedAdmin:FullName"] ?? "Main Admin";

            if (string.IsNullOrWhiteSpace(mainAdminEmail) || string.IsNullOrWhiteSpace(mainAdminPassword))
            {
                logger.LogWarning(
                    "SeedAdmin:Email or SeedAdmin:Password is not configured. Skipping MainAdmin seeding.");
                return;
            }

            try
            {
                var existingUser = await userManager.FindByEmailAsync(mainAdminEmail);
                if (existingUser == null)
                {
                    var mainAdmin = new ApplicationUser
                    {
                        UserName = mainAdminEmail,
                        Email = mainAdminEmail,
                        FullName = mainAdminFullName,
                        PhoneNumber = mainAdminPhone,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(mainAdmin, mainAdminPassword);
                    if (result.Succeeded)
                    {
                        var roleResult = await userManager.AddToRoleAsync(mainAdmin, "MainAdmin");
                        if (!roleResult.Succeeded)
                        {
                            foreach (var error in roleResult.Errors)
                                logger.LogError("Failed to assign MainAdmin role: {Code} - {Description}",
                                    error.Code, error.Description);
                        }
                        else
                        {
                            logger.LogInformation("MainAdmin seeded successfully: {Email}", mainAdminEmail);
                        }
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                            logger.LogError("MainAdmin seeding error: {Code} - {Description}",
                                error.Code, error.Description);
                    }
                }
                else
                {
                    logger.LogInformation("MainAdmin already exists. Skipping creation.");
                }
            }
            catch (Exception ex)
            {
                LogFullException(logger, ex, "Failed seeding MainAdmin user");
                throw;
            }
        }

        private static async Task SeedDefaultAdminAsync(
            UserManager<ApplicationUser> userManager,
            ILogger logger)
        {
            const string defaultAdminEmail = "bibekydv285@gmail.com";
            const string defaultAdminPassword = "9763244805";
            const string defaultAdminFullName = "Bibek Admin";

            try
            {
                var existingUser = await userManager.FindByEmailAsync(defaultAdminEmail);
                if (existingUser == null)
                {
                    var defaultAdmin = new ApplicationUser
                    {
                        UserName = defaultAdminEmail,
                        Email = defaultAdminEmail,
                        FullName = defaultAdminFullName,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(defaultAdmin, defaultAdminPassword);
                    if (result.Succeeded)
                    {
                        var roleResult = await userManager.AddToRoleAsync(defaultAdmin, "Admin");
                        if (!roleResult.Succeeded)
                        {
                            foreach (var error in roleResult.Errors)
                                logger.LogError("Failed to assign Admin role: {Code} - {Description}",
                                    error.Code, error.Description);
                        }
                        else
                        {
                            logger.LogInformation("Default Admin seeded successfully: {Email}", defaultAdminEmail);
                        }
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                            logger.LogError("Default Admin seeding error: {Code} - {Description}",
                                error.Code, error.Description);
                    }
                }
                else
                {
                    // Ensure existing user has Admin role
                    var roles = await userManager.GetRolesAsync(existingUser);
                    if (!roles.Contains("Admin"))
                    {
                        var roleResult = await userManager.AddToRoleAsync(existingUser, "Admin");
                        if (roleResult.Succeeded)
                        {
                            logger.LogInformation("Added Admin role to existing user: {Email}", defaultAdminEmail);
                        }
                        else
                        {
                            foreach (var error in roleResult.Errors)
                                logger.LogError("Failed to add Admin role to existing user: {Code} - {Description}",
                                    error.Code, error.Description);
                        }
                    }
                    logger.LogInformation("Default Admin already exists. Skipping creation.");
                }
            }
            catch (Exception ex)
            {
                LogFullException(logger, ex, "Failed seeding Default Admin user");
                throw;
            }
        }

        private static async Task SeedInitialDataAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                if (!context.Subjects.Any())
                {
                    var subjects = new List<Subject>
                    {
                        new Subject { Name = "English", Code = "ENG", Description = "English Language and Literature", IsCoreSubject = true, FullMarks = 100, PassMarks = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new Subject { Name = "Mathematics", Code = "MATH", Description = "Mathematics", IsCoreSubject = true, FullMarks = 100, PassMarks = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new Subject { Name = "Science", Code = "SCI", Description = "General Science", IsCoreSubject = true, FullMarks = 100, PassMarks = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new Subject { Name = "Social Studies", Code = "SST", Description = "Social Studies", IsCoreSubject = true, FullMarks = 100, PassMarks = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new Subject { Name = "Nepali", Code = "NEP", Description = "Nepali Language", IsCoreSubject = true, FullMarks = 100, PassMarks = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new Subject { Name = "Computer Science", Code = "CS", Description = "Computer Science", IsCoreSubject = false, FullMarks = 100, PassMarks = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new Subject { Name = "Health & Physical Education", Code = "HPE", Description = "Health and Physical Education", IsCoreSubject = false, FullMarks = 100, PassMarks = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
                        new Subject { Name = "Optional Mathematics", Code = "OMATH", Description = "Optional Mathematics", IsCoreSubject = false, FullMarks = 100, PassMarks = 40, IsActive = true, CreatedAt = DateTime.UtcNow }
                    };

                    context.Subjects.AddRange(subjects);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} subjects.", subjects.Count);
                }
            }
            catch (Exception ex)
            {
                LogFullException(logger, ex, "Failed seeding Subjects");
                throw;
            }

            try
            {
                if (!context.SchoolClasses.Any())
                {
                    var classes = new List<SchoolClass>
                    {
                        new SchoolClass { Name = "Grade 1", Section = "A", Description = "Grade 1 Section A", Capacity = 30, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 1", Section = "B", Description = "Grade 1 Section B", Capacity = 30, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 2", Section = "A", Description = "Grade 2 Section A", Capacity = 35, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 2", Section = "B", Description = "Grade 2 Section B", Capacity = 35, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 3", Section = "A", Description = "Grade 3 Section A", Capacity = 40, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 4", Section = "A", Description = "Grade 4 Section A", Capacity = 40, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 5", Section = "A", Description = "Grade 5 Section A", Capacity = 40, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 6", Section = "A", Description = "Grade 6 Section A", Capacity = 40, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 7", Section = "A", Description = "Grade 7 Section A", Capacity = 40, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 8", Section = "A", Description = "Grade 8 Section A", Capacity = 40, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 9", Section = "A", Description = "Grade 9 Section A", Capacity = 40, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new SchoolClass { Name = "Grade 10", Section = "A", Description = "Grade 10 Section A", Capacity = 40, AcademicYear = "2024-2025", IsActive = true, CreatedAt = DateTime.UtcNow }
                    };

                    context.SchoolClasses.AddRange(classes);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} classes.", classes.Count);
                }
            }
            catch (Exception ex)
            {
                LogFullException(logger, ex, "Failed seeding SchoolClasses");
                throw;
            }
        }
    }
}