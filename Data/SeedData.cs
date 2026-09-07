using CloudDesk.cs.Models;
using CloudDesk.Models;
using Microsoft.AspNetCore.Identity;

namespace CloudDesk.Data
{
    public static class SeedData
    {
        public static async Task CreateAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleManager =
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Create roles
            string[] roles = { "ChatUser", "Viewer" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // User A
            await CreateUser(
                userManager,
                "usera@clouddesk.local",
                "UserA",
                "UserA@123",
                "ChatUser");

            // User B
            await CreateUser(
                userManager,
                "userb@clouddesk.local",
                "UserB",
                "UserB@123",
                "ChatUser");

            // User C - Viewer
            await CreateUser(
                userManager,
                "userc@clouddesk.local",
                "UserC",
                "UserC@123",
                "Viewer");
        }

        private static async Task CreateUser(
            UserManager<ApplicationUser> userManager,
            string email,
            string displayName,
            string password,
            string role)
        {
            var existingUser = await userManager.FindByEmailAsync(email);

            if (existingUser != null)
                return;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName
            };

            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}