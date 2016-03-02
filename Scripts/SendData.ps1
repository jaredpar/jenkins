param([switch]$real = $false)

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

$url = "http://localhost:9859"
if ($real) {
    $url = "http://jdash.azurewebsites.net"
}
    
$dataJson = ConvertTo-Json $data
Invoke-RestMethod "$url/api/testCache/hello" -method put -contenttype application/json -body $dataJson
