# Copyright (c) Files Community
# Licensed under the MIT License.

# Abstract:
#  This script analyzes the trx file that is the result of executing dotnet test and
#  converts it to markdown, which is used for the Files CI.

using namespace System.Collections.Generic;
using namespace System.Linq;

param(
    [string]$Source = "",
    [string]$Destination = ""
)

class Trx
{
    [ResultSummary] $ResultSummary
    [TrxTimes] $Times
    [TrxTestDefinition[]]$TestDefinitions
    [TrxUnitTestResult[]]$Results

    Trx()
    {
        $this.Times = New-Object TrxTimes
        $this.ResultSummary = New-Object ResultSummary
    }
}

class ResultSummary
{
    [int] $total
    [int] $passed
    [int] $failed
}

class TrxTimes
{
    [Datetime] $creation
    [Datetime] $queuing
    [Datetime] $start
    [Datetime] $finish
}

class TrxTestDefinition
{
    [string] $id
    [string] $name
    [string] $className
}

class TrxUnitTestResult
{
    [string] $id
    [string] $result
    [string] $name
    [string] $errorMessage
    [string] $errorStackTrace
}

[xml]$xmlDoc = Get-Content $Source

$trxObject = New-Object Trx

# Result summary
$trxObject.ResultSummary.total = $xmlDoc.TestRun.ResultSummary.Counters.total
$trxObject.ResultSummary.passed = $xmlDoc.TestRun.ResultSummary.Counters.passed
$trxObject.ResultSummary.failed = $xmlDoc.TestRun.ResultSummary.Counters.failed

# Times
$trxObject.Times.start = $xmlDoc.TestRun.Times.start
$trxObject.Times.finish = $xmlDoc.TestRun.Times.finish

# Unit test definitions
foreach ($item in $xmlDoc.TestRun.TestDefinitions.UnitTest)
{
    $newOne = [TrxTestDefinition]::new()

    $newOne.id = $item.Execution.id
    $newOne.name = $item.name
    $newOne.className = $item.TestMethod.className

    $trxObject.TestDefinitions += $newOne
}

# Unit tests
foreach ($item in $xmlDoc.TestRun.Results.UnitTestResult)
{
    $newOne = [TrxUnitTestResult]::new()

    $newOne.id = $item.executionId
    $newOne.result = $item.outcome
    $newOne.name = $item.testName
    $newOne.errorMessage = $item.Output.ErrorInfo.Message
    $newOne.errorStackTrace = $item.Output.ErrorInfo.StackTrace

    $trxObject.Results += $newOne
}

$stringBuilder = New-Object System.Text.StringBuilder

$title = "## Tested with Files <img alt=""logo"" src=""https://github.com/files-community/Files/assets/62196528/6d4b489a-12be-4819-9bbc-a5f95858e77d"" width=""28"" align=""top"" />"
$AbbreviatedOid = $env:GITHUB_SHA
$AbbreviatedOid = $AbbreviatedOid.Substring(0, 7)
$resultOverview = ""

# Header
[void]$stringBuilder.AppendLine($title);
[void]$stringBuilder.AppendLine("");

$skippedIcon = "⏩"
$passedIcon = "✅"
$failedIcon = "❌"
$unknownIcon = "⚠️"

# Skipped
if ($trxObject.ResultSummary.failed -ne 0 -and $trxObject.ResultSummary.passed -ne 0)
{
    $resultOverview = $partialSuccessfulfulIcon + "Some tests were not successful"
}
# Passed
elseif ($trxObject.ResultSummary.failed -eq 0)
{
    $resultOverview = $passedIcon + " All tests have passed"
}
# Failed
elseif ($trxObject.ResultSummary.failed -ne 0 -and $trxObject.ResultSummary.passed -eq 0)
{
    $resultOverview = $failedIcon + " All tests have failed"
}
else
{
    $resultOverview = $unknownIcon + " Unknown result"
}

# Overview table
[void]$stringBuilder.AppendLine("<table>");
[void]$stringBuilder.AppendLine("<tr>");
[void]$stringBuilder.AppendLine("<td><strong>Commit:</strong></td>");
[void]$stringBuilder.AppendLine("<td><a href=""https://github.com/files-community/Files/pull/$env:PULL_REQUEST_ID/commits/$AbbreviatedOid""><code>$AbbreviatedOid</code></a></td>");
[void]$stringBuilder.AppendLine("</tr>");
[void]$stringBuilder.AppendLine("<tr>");
[void]$stringBuilder.AppendLine("<td><strong>Status:</strong></td>");
[void]$stringBuilder.AppendLine("<td>$resultOverview</td>");
[void]$stringBuilder.AppendLine("</tr>");
[void]$stringBuilder.AppendLine("<tr>");
[void]$stringBuilder.AppendLine("<td><strong>Full log:</strong></td>");
[void]$stringBuilder.AppendLine("<td><a href=""https://github.com/files-community/Files/actions/runs/$env:GITHUB_RUN_ID/job/$env:GITHUB_JOB"">https://github.com/files-community/Files/actions/runs/$GITHUB_RUN_ID/job/$GITHUB_JOB</a></td>");
[void]$stringBuilder.AppendLine("</tr>");
[void]$stringBuilder.AppendLine("</table>");
[void]$stringBuilder.AppendLine("");

if ($trxObject.ResultSummary.failed -eq 0)
{
    $stringBuilder.ToString() | Out-File -FilePath $Destination
    Exit
}

# Details Table
[void]$stringBuilder.AppendLine("### Details");
[void]$stringBuilder.AppendLine("Name|Status|Failed class");
[void]$stringBuilder.AppendLine(":---|:---|:---");

$index = 0

foreach ($item in $trxObject.Results)
{
    $resultStatus = ""
    if ($item.result.Equals("Failed"))
    {
        $resultStatus = $failedIcon + " Failed"
    }
    elseif ($item.result.Equals("Passed"))
    {
        $resultStatus = $successfulIcon + " Passed"
    }
    else
    {
        $resultStatus = $skippedIcon + " Unknown"
    }

    $failedClassName = "_None_"
    $testName = $item.name
    $baseClassName = $trxObject.TestDefinitions | Where-Object { $_.id -eq $item.id }
    if ($null -ne $item.errorMessage)
    {
        $failedClassName = "<code>" + $baseClassName.className + "." + $item.name + "</code>"
    }

    [void]$stringBuilder.AppendLine("$testName`|$resultStatus`|$failedClassName");

    $index++
}

[void]$stringBuilder.AppendLine("");

[void]$stringBuilder.AppendLine("<details>");
[void]$stringBuilder.AppendLine("<summary>View logs</summary>");
[void]$stringBuilder.AppendLine("<br/>");
[void]$stringBuilder.AppendLine("");

foreach ($item in $trxObject.Results)
{
    $baseClassName = $trxObject.TestDefinitions | Where-Object { $_.id -eq $item.id }
    $failedClassName = $baseClassName.className + "." + $item.name
    [void]$stringBuilder.AppendLine("- **" + $failedClassName + "**");

    $stackTraceText = $item.errorStackTrace
    [void]$stringBuilder.AppendLine("  ``````");
    [void]$stringBuilder.Append("$stackTraceText");
    [void]$stringBuilder.AppendLine("  ``````");
}

[void]$stringBuilder.AppendLine("");
[void]$stringBuilder.AppendLine("</details>");
[void]$stringBuilder.AppendLine("");


$stringBuilder.ToString() | Out-File -FilePath $Destination
