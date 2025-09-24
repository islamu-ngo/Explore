var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Explore_Blazor>("explore-blazor");

builder.Build().Run();
