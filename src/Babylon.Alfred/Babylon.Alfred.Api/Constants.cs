namespace Babylon.Alfred.Api;

public class Constants
{
    public struct User
    {
        // This constant is used to identify the root user.
        // Will be removed in the future when proper user management is implemented.
        public static readonly Guid RootUserId = Guid.Parse("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d");
    }
}
