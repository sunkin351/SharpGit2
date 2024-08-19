using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2
{
    public unsafe struct GitSignature
    {
        public string? Name;
        public string? Email;
        public GitTime When;

        public GitSignature(in Native.GitSignature sig)
        {
            Name = Utf8StringMarshaller.ConvertToManaged(sig.Name);
            Email = Utf8StringMarshaller.ConvertToManaged(sig.Email);
            When = sig.When;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitSignature
    {
        public byte* Name;
        public byte* Email;
        public GitTime When;

        public void FromManaged(SharpGit2.GitSignature signature)
        {
            Name = Utf8StringMarshaller.ConvertToUnmanaged(signature.Name);
            Email = Utf8StringMarshaller.ConvertToUnmanaged(signature.Email);
            When = signature.When;
        }

        public void Free()
        {
            Utf8StringMarshaller.Free(Name);
            Utf8StringMarshaller.Free(Email);
        }

        public static GitSignature* FromManaged(SharpGit2.GitSignature? signature)
        {
            if (!signature.HasValue)
                return null;

            var memory = (GitSignature*)NativeMemory.AllocZeroed((nuint)sizeof(GitSignature));

            try
            {
                memory->FromManaged(signature.GetValueOrDefault());
                return memory;
            }
            catch
            {
                memory->Free();
                NativeMemory.Free(memory);
                throw;
            }
        }
    }
}
