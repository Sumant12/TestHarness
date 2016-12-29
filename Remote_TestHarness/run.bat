cd TestHarness\bin\Debug
start .\TestHarness.exe 8085 8083
cd /Repository\bin\Debug
start .\Repository.exe 8083
cd ClientWPF\bin\Debug
start .\ClientWPF.exe 8084 8085 8083
cd Client\bin\Debug
start .\Client.exe 8085 8085 8083
cd Client2\bin\Debug
start .\Client2.exe 8085 8085 8083
cd ../../../
@pause