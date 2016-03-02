param([switch]$real = $false)

function Get-MD5($text)  {
    $md5 = new-object -TypeName System.Security.Cryptography.MD5CryptoServiceProvider
    $utf8 = new-object -TypeName System.Text.UTF8Encoding
    $hash = [System.BitConverter]::ToString($md5.ComputeHash($utf8.GetBytes($text)))

    #to remove hyphens and downcase letters add:
    $hash = $hash.ToLower() -replace '-', ''
    return $hash
}

$id = Get-MD5 ([Guid]::NewGuid().ToString())

$data = @{
    testResultData = @{
        exitCode = 42;
        outputStandard = "";
        outputError = "";
        resultsFileContent = "hello";
        resultsFileName = "test.xml";
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
Invoke-RestMethod "$url/api/testCache/$id" -method put -contenttype application/json -body $dataJson
$result = Invoke-WebRequest "$url/api/testCache/$id" -method get
if ($result.StatusCode -ne 200) {
    write-host "Could not retrieve resource"
    $result;
}
