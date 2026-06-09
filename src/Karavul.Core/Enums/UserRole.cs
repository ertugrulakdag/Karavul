namespace Karavul.Core.Enums;

[Flags]
public enum UserRole
{
    Admin = 1,
    Editor = 2,
    Operator = 4,
    Viewer = 8
}
