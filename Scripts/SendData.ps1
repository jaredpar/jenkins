
$data = @{
    testResultData = @{
        exitCode = 42;
        outputStandard = "";
        outputError = "";
        resultsFileContent = "hello";
        resultsFileName = "result.xml";
    };
    testSourceData = @{
        machineName = "a machine";
        testRoot = "c:\blah";
    };
}

$dataJson = ConvertTo-Json $data
Invoke-RestMethod http://localhost:9859/api/testCache/hello -method put -contenttype application/json -body $dataJson
