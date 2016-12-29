///////////////////////////////////////////////////////////////////////
// IFileService.cs - Interface for self-hosted file transfer service //
//                                                                   //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2010   //
///////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace TestHarness
{
    [ServiceContract(Namespace = "TestHarness")]
    public interface IFileService
    {
        [OperationContract]
        bool OpenFileForWrite(string name);

        [OperationContract]
        bool WriteFileBlock(byte[] block);

        [OperationContract]
        bool CloseFile();
    }
}
