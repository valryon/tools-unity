/* MIT License
    Copyright (c) 2016 RedBlueGames
    Code written by Doug Cox
    */

using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

/// <summary>
/// GitException includes the error output from a Git.Run() command as well as the
/// ExitCode it returned.
/// </summary>
/// <remarks>https://gist.github.com/edwardrowe/fdec706fe53bfff0671e063f263ada63</remarks>
public class GitException : InvalidOperationException
{
    public GitException(int exitCode, string errors) : base(errors) =>
        ExitCode = exitCode;

    /// <summary>
    /// The exit code returned when running the Git command.
    /// </summary>
    public readonly int ExitCode;
}

public static class Git
{
    /// <summary>
    /// The currently active branch.
    /// </summary>
    public static string Branch => Run(@"rev-parse --abbrev-ref HEAD");

    /// <summary>
    /// Returns a listing of all uncommitted or untracked (added) files.
    /// </summary>
    public static string Status => Run(@"status --porcelain");


    public static string Log => Run(@"log --oneline");

    // https://stackoverflow.com/questions/1441010/the-shortest-possible-output-from-git-log-containing-author-and-date
    // %h = abbreviated commit hash
    // %x09 = tab (character for code 9)
    // %an = author name
    // %ad = author date (format respects --date= option)
    // %s = subject
    /// <summary>
    /// Log 150 commits with specific format hash\tauthor\tmessage
    /// </summary>
    public static string LogWithFormat => Run("log HEAD~150.. --pretty=format:\"%h%x09%an%x09%s\"");

    /// <summary>
    /// Runs git exe with the specified arguments and returns the output.
    /// </summary>
    public static string Run(string arguments, int timeout = 10000)
    {
        using (var process = new Process
               {
                   StartInfo =
                   {
                       FileName = "git",
                       Arguments = arguments,
                       CreateNoWindow = true,
                       UseShellExecute = false,
                       RedirectStandardOutput = true,
                       RedirectStandardError = true
                   }
               })
        {
            var output = new StringBuilder();
            var error = new StringBuilder();

            using (var outputWaitHandle = new AutoResetEvent(false))
            using (var errorWaitHandle = new AutoResetEvent(false))
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (process.WaitForExit(timeout) &&
                    outputWaitHandle.WaitOne(timeout) &&
                    errorWaitHandle.WaitOne(timeout))
                {
                    if (process.ExitCode == 0)
                    {
                        return output.ToString();
                    }
                    else
                    {
                        throw new GitException(process.ExitCode, error.ToString());
                    }

                    // Process completed. Check process.ExitCode here.
                }
                else
                {
                    process.CancelOutputRead();
                    throw new GitException(1, "Timeout");
                }
            }
        }
    }

    /// <summary>
    /// Runs git exe asynchronously with the specified arguments and returns the output.
    /// </summary>
    public static async Task<string> RunAsync(string arguments, int timeout = 10000)
    {
        return await Task.Run(() => Run(arguments, timeout));
    }

    public static string GetShortCommitHash()
    {
        var commit = Run("rev-parse HEAD");
        return Regex.Replace(commit.Substring(0, 9), @"\t|\n|\r", "");
    }
}