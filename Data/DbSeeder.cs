using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BibekSchool.Models;

namespace BibekSchool.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            await SeedRolesAsync(roleManager);
            await SeedMainAdminAsync(userManager);
            await SeedInitialDataAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "MainAdmin", "Admin", "Teacher", "Student" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedMainAdminAsync(UserManager<ApplicationUser> userManager)
        {
            const string mainAdminEmail = "bibekydv285@gmail.com";
            const string mainAdminPassword = "9763244805";

            var existingUser = await userManager.FindByEmailAsync(mainAdminEmail);
            if (existingUser == null)
            {
                var mainAdmin = new ApplicationUser
                {
                    UserName = mainAdminEmail,
                    Email = mainAdminEmail,
                    FullName = "Bibek Yadav",
                    PhoneNumber = "9763244805",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(mainAdmin, mainAdminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(mainAdmin, "MainAdmin");
                }
            }
        }

        private static async Task SeedInitialDataAsync(ApplicationDbContext context)
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
            }

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
            }
        }
    }
}