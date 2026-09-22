using Microsoft.AspNetCore.Identity;
using InteractHub.Application.Entities;
using InteractHub.Application.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Infrastructure.Data;

public static class DbInitializer
{
    /// <summary>
    /// Seed Roles (Admin, User, Moderator) và tạo Admin user mặc định
    /// </summary>
    public static async Task SeedRolesAndAdmin(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        // ✅ Bước 1: Tạo Roles
        string[] roles = { RoleConstants.Admin, RoleConstants.User, RoleConstants.Moderator };
        
        foreach (var role in roles)
        {
            // Kiểm tra role đã tồn tại chưa
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                {
                    Console.WriteLine($"✅ Role '{role}' created successfully");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to create role '{role}'");
                }
            }
        }

        // ✅ Bước 2: Tạo Admin User mặc định (nếu chưa tồn tại)
        var adminEmail = "admin@interacthub.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = "admin",
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, "Admin@123456");
            
            if (createResult.Succeeded)
            {
                // Gán role Admin cho admin user
                var roleResult = await userManager.AddToRoleAsync(adminUser, RoleConstants.Admin);
                if (roleResult.Succeeded)
                {
                    Console.WriteLine($"✅ Admin user created with role '{RoleConstants.Admin}'");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to assign role to admin user");
                }
            }
            else
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                Console.WriteLine($"❌ Failed to create admin user: {errors}");
            }
        }
    }

    /// <summary>
    /// Seed test data: 5 users, 3 posts per user, 3 groups with random members (min 2 per group)
    /// </summary>
    public static async Task SeedTestData(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var random = new Random();

        // ✅ Bước 1: Tạo 5 users (nếu chưa tồn tại)
        var testUsers = new List<User>();
        var userEmails = new[] 
        { 
            "user1@interacthub.com",
            "user2@interacthub.com", 
            "user3@interacthub.com",
            "user4@interacthub.com",
            "user5@interacthub.com"
        };

        var userNames = new[]
        {
            "John Doe",
            "Jane Smith",
            "Michael Johnson",
            "Sarah Williams",
            "David Brown"
        };

        for (int i = 0; i < userEmails.Length; i++)
        {
            var existingUser = await userManager.FindByEmailAsync(userEmails[i]);
            
            if (existingUser == null)
            {
                var newUser = new User
                {
                    UserName = $"user{i + 1}",
                    Email = userEmails[i],
                    FullName = userNames[i],
                    EmailConfirmed = true,
                    Bio = $"Hi, I'm {userNames[i]}! 👋"
                };

                var createResult = await userManager.CreateAsync(newUser, "User@123456");
                
                if (createResult.Succeeded)
                {
                    // Gán role User
                    await userManager.AddToRoleAsync(newUser, RoleConstants.User);
                    testUsers.Add(newUser);
                    Console.WriteLine($"✅ User '{userEmails[i]}' created successfully");
                }
                else
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    Console.WriteLine($"❌ Failed to create user: {errors}");
                }
            }
            else
            {
                testUsers.Add(existingUser);
            }
        }

        // ✅ Bước 2: Tạo 3 posts per user (15 posts tổng)
        var postContents = new[]
        {
            "Just finished a great project! 🚀",
            "Beautiful sunset today ☀️",
            "Coffee and coding, the perfect combo ☕💻",
            "Just launched my new blog post!",
            "Learning something new every day 📚",
            "Team lunch was amazing! 🍽️",
            "Starting a new project with the team",
            "Made some progress on my goals today!",
            "Excited about the upcoming events 🎉",
            "Day 100 of my fitness journey 💪",
            "Finally finished that book I was reading",
            "Celebrating a work milestone with the team",
            "New ideas for improving our workflow",
            "Great meeting with amazing collaborators",
            "Taking a well-deserved break this weekend ✨"
        };

        var existingPostCount = await dbContext.Posts.CountAsync();
        if (existingPostCount == 0)
        {
            int postIndex = 0;
            foreach (var user in testUsers)
            {
                for (int i = 0; i < 3; i++)
                {
                    var post = new Post
                    {
                        Content = postContents[postIndex % postContents.Length],
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-(testUsers.Count * 3 - postIndex))
                    };

                    dbContext.Posts.Add(post);
                    postIndex++;
                }
            }

            await dbContext.SaveChangesAsync();
            Console.WriteLine($"✅ Created {testUsers.Count * 3} posts successfully");
        }

        // ✅ Bước 3: Tạo 3 groups
        var groupNames = new[] { "Tech Enthusiasts", "Photography Lovers", "Fitness Community" };
        var groupSlugs = new[] { "tech-enthusiasts", "photography-lovers", "fitness-community" };
        var groupDescriptions = new[]
        {
            "A group for technology enthusiasts and developers",
            "Share beautiful photos and photography tips",
            "Support each other on fitness journeys"
        };

        var existingGroups = await dbContext.Groups.ToListAsync();
        var createdGroups = new List<Group>();

        if (existingGroups.Count == 0)
        {
            for (int i = 0; i < groupNames.Length; i++)
            {
                var group = new Group
                {
                    Name = groupNames[i],
                    Slug = groupSlugs[i],
                    Description = groupDescriptions[i],
                    CreatorId = testUsers[i].Id,
                    Creator = testUsers[i],
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Groups.Add(group);
                createdGroups.Add(group);
            }

            await dbContext.SaveChangesAsync();
            Console.WriteLine($"✅ Created {createdGroups.Count} groups successfully");
        }
        else
        {
            createdGroups = existingGroups;
        }

        // ✅ Bước 4: Randomly assign users to groups (minimum 2 per group)
        var existingMemberships = await dbContext.GroupMemberships.CountAsync();
        if (existingMemberships == 0)
        {
            // Đảm bảo mỗi group có ít nhất 2 members
            // Với 5 users và 3 groups: 2, 2, 1 hoặc 2, 2, 1 hoặc 3, 1, 1 (nhưng cần min 2 mỗi group)
            // Phân bố ngẫu nhiên nhưng đảm bảo min 2
            var userIds = testUsers.Select(u => u.Id).ToList();
            
            // Shuffle users
            for (int i = userIds.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);
                var temp = userIds[i];
                userIds[i] = userIds[randomIndex];
                userIds[randomIndex] = temp;
            }

            // Phân bố: Group 1 = 2 users, Group 2 = 2 users, Group 3 = 1 user
            // Nhưng Group 3 cần tối thiểu 2, nên sẽ là: 2, 2, 1... wait, cần min 2 mỗi group
            // Với 5 users và 3 groups với min 2 mỗi group, ta không thể phân bố đều
            // Sử dụng: 2, 2, 1 từ perspective nhưng thêm logic để đảm bảo min 2
            // Cách tốt nhất: Phân bố ngẫu nhiên nhưng assign tối thiểu 2 cho mỗi group, sau đó phân phối phần còn lại
            
            int[][] userAssignments = new int[3][];
            
            // Bắt đầu với 2 users cho mỗi group
            userAssignments[0] = new[] { 0, 1 };
            userAssignments[1] = new[] { 2, 3 };
            userAssignments[2] = new[] { 4 };
            
            // Nếu group 2 chỉ có 1 user, thêm ngẫu nhiên
            if (userAssignments[2].Length < 2)
            {
                var availableUsers = Enumerable.Range(0, userIds.Count)
                    .Where(i => !userAssignments[0].Contains(i) && !userAssignments[1].Contains(i))
                    .ToList();
                
                if (availableUsers.Count > 0)
                {
                    var randomUser = availableUsers[random.Next(availableUsers.Count)];
                    userAssignments[2] = userAssignments[2].Append(randomUser).ToArray();
                }
            }

            // Thêm group memberships
            for (int g = 0; g < createdGroups.Count; g++)
            {
                foreach (var userIndex in userAssignments[g])
                {
                    var membership = new GroupMembership
                    {
                        GroupId = createdGroups[g].Id,
                        UserId = userIds[userIndex],
                        JoinedAt = DateTime.UtcNow
                    };

                    dbContext.GroupMemberships.Add(membership);
                }
            }

            await dbContext.SaveChangesAsync();
            
            foreach (var group in createdGroups)
            {
                var memberCount = await dbContext.GroupMemberships
                    .Where(gm => gm.GroupId == group.Id)
                    .CountAsync();
                Console.WriteLine($"✅ Group '{group.Name}' has {memberCount} members");
            }
        }
    }
}
