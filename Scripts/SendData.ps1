param([switch]$real = $false)

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

function Test-TestCache() {

    $id = Get-MD5 ([Guid]::NewGuid().ToString())

    $data = @{
        testResultData = @{
            exitCode = 42;
            outputStandard = "";
            outputError = "";
            resultsFileContent = "<html><body><h2>hello world</h2></body></html>";
            resultsFileName = "test.html";
            ellapsedSeconds = 100;
        };
        testSourceData = @{
            machineName = "a machine";
            testRoot = "c:\blah";
        };
    }

    $dataJson = ConvertTo-Json $data
    Invoke-RestMethod "$url/api/testCache/$id" -method put -contenttype application/json -body $dataJson
    $result = Invoke-WebRequest "$url/api/testCache/$id" -method get
    if ($result.StatusCode -ne 200) {
        write-host "Could not retrieve resource"
        $result;
    }
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

Test-TestCache
# Test-TestRun
