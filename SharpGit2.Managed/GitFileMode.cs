namespace SharpGit2.Managed;

public enum GitFileMode
{
    Unreadable = 0,
    Tree = 0x4000,
    Blob = 0x81A4,
    BlobExecutable = 0x81ED,
    Link = 0xA000,
    Commit = 0xE000,
}
