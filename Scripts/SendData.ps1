param([switch]$real = $false)
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

$url = "http://localhost:9859"
if ($real) {
    $url = "http://jdash.azurewebsites.net"
}

function Get-MD5($text)  {
    $md5 = new-object -TypeName System.Security.Cryptography.MD5CryptoServiceProvider
    $utf8 = new-object -TypeName System.Text.UTF8Encoding
    $hash = [System.BitConverter]::ToString($md5.ComputeHash($utf8.GetBytes($text)))

    #to remove hyphens and downcase letters add:
    $hash = $hash.ToLower() -replace '-', ''
    return $hash
}

function Get-Id() {
    Get-MD5 ([Guid]::NewGuid().ToString())
}

function Test-Values() {
    param ($msg, $left, $right)

    if ($left -ne $right) {
        write-host "Error $msg!!!: $left not equal $right "
    }
}

function Test-TestCacheCore() {
    param ( $route = $(throw "Need a route"),
            $data = $(throw "Need the JSON data"))

    $id = Get-Id
    write-host "Testing result cache $id in $route"
    $dataJson = ConvertTo-Json $data
    Invoke-RestMethod "$url/$route/$id" -method put -contenttype application/json -body $dataJson

    $requestUri = "$url/$route/$($id)?machineName=jaredpar03&enlistmentRoot=foo"
    $result = Invoke-WebRequest $requestUri -method get
    if ($result.StatusCode -ne 200) {
        write-host "Could not retrieve resource"
        $result
        return
    }

    $oldData = $data.testResultData
    $newData = ConvertFrom-Json $result.Content
    Test-Values "exitCode" $oldData.ExitCode $newData.ExitCode 
    Test-Values "elapsedSeconds" $oldData.elapsedSeconds $newData.ElapsedSeconds 
    Test-Values "content" $oldData.ResultsFileContent $newData.ResultsFileContent
}

function Test-TestCache() {
    param ( $route = $(throw "Need a route"))
    $data = @{
        testResultData = @{
            exitCode = 42;
            outputStandard = "";
            outputError = "";
            resultsFileContent = "<html><body><h2>hello world</h2></body></html>";
            resultsFileName = "test.html";
            elapsedSeconds = 100;
            testPassed = 1;
            testFailed = 2;
            testSkipped = 3;
        };
        testSourceData = @{
            machineName = "jaredpar03";
            enlistmentRoot = "c:\blah";
            assemblyName = "test.dll";
        };
    }

    Test-TestCacheCore $route $data

    $data.testResultData.Remove("testPassed");
    $data.testResultData.Remove("testFailed");
    $data.testResultData.Remove("testSkipped");
    Test-TestCacheCore $route $data

    $data.testResultData.outputError = $null
    Test-TestCacheCore $route $data

    $data.testResultData.outputStandard = $null
    Test-TestCacheCore $route $data

    $data.testSourceData.Remove("machineName");
    $data.testSourceData.Remove("enlistmentRoot");
    Test-TestCacheCore $route $data
}

function Test-TestRun() {

    $data = @{
        EllapsedSeconds = 42;
        IsJenkins = $false;
        Is32Bit = $true;
        CacheCount = 42;
        AssemblyCount = 42;
        Cache = "test";
        Succeeded = $true;
    }

    $dataJson = ConvertTo-Json $data
    $result = Invoke-WebRequest "$url/api/testRun" -method post -contenttype application/json -body $dataJson
    if ($result.StatusCode -ne 204) {
        write-host "Could not post resource"
        $result;
    }
}

Test-TestCache "api/testCache"
Test-TestCache "api/testData/cache"
Test-TestRun
