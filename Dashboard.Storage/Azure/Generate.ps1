

function Generate-File()
{
    $map = @{
        "byte[]" = "ForBinary";
        "bool" = "ForBool";
        "DateTimeOffset" = "ForDate";
        "double" = "ForDouble";
        "Guid" = "ForGuid";
        "int" = "ForInt";
        "long" = "ForLong";
        "string" = "";
    }

    $header = 
@"
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Dashboard.Azure
{
    public static partial class TableQueryUtil
    {
"@;

    write-output $header


    foreach ($type in $map.Keys) {
        $name = $map[$type]
        

        $item = 
@"
        
        public static string Column(string columnName, $type value, ColumnOperator op = ColumnOperator.Equal)
        {
            return TableQuery.GenerateFilterCondition$name(
                columnName,
                ToQueryComparison(op),
                value);
        }
"@;
        write-output $item
    }

    $footer = 
@"
    }
}
"@

    write-output $footer
}

Generate-File | out-file "TableQueryUtil.Generated.cs" -encoding utf8
