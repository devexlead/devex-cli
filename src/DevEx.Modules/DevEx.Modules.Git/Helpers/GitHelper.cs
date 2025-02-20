﻿using DevEx.Core.Helpers;
using DevEx.Core.Storage;
using DevEx.Core.Storage.Model;
using DevEx.Integrations.GitHub;
using Spectre.Console;
using TextCopy;
using static DevEx.Core.Helpers.TerminalHelper;

namespace DevEx.Modules.Git.Helpers
{
    public static class GitHelper
    {
        public static void ConfigureProfile()
        {
            var gitUsername = AnsiConsole.Ask<string>("Git Username: ");
            var gitEmail = AnsiConsole.Ask<string>("Git Email Address: ");

            //Global Profile
            TerminalHelper.Run(ConsoleMode.Powershell, "git config --global user.name");
            TerminalHelper.Run(ConsoleMode.Powershell, $"git config --global user.email {gitEmail}");
            TerminalHelper.Run(ConsoleMode.Powershell, "git config --global user.email");

            //Set up Local Profile for all repos
            var repositories = GetRepositories();

            foreach (var repo in repositories)
            {
                TerminalHelper.Run(ConsoleMode.Powershell, $"git config --local user.name {gitUsername}", repo.WorkingFolder);
                TerminalHelper.Run(ConsoleMode.Powershell, "git config --local user.name", repo.WorkingFolder);
                TerminalHelper.Run(ConsoleMode.Powershell, $"git config --local user.email {gitEmail}", repo.WorkingFolder);
                TerminalHelper.Run(ConsoleMode.Powershell, "git config --local user.email", repo.WorkingFolder);
            }
        }

        /// <summary>
        /// Clone all repositories for the development working folder
        /// </summary>
        public static void Clone()
        {
            var repositories = GetRepositories();

            foreach (var repository in repositories)
            {
                if (!Directory.Exists(repository.WorkingFolder))
                {
                    Directory.CreateDirectory(repository.WorkingFolder);
                }

                TerminalHelper.Run(ConsoleMode.Powershell, $"git clone --branch {repository.DefaultBranch} {repository.RemoteLocation} {repository.WorkingFolder}");
            }
        }

        /// <summary>
        /// Clone a specific repository to a specific working folder
        /// </summary>
        /// <param name="repoName"></param>
        /// <param name="workingFolder"></param>
        public static void Clone(string repoName, string workingFolder)
        {
            var repository = GetRepositories().FirstOrDefault(r => r.Name.Equals(repoName));
            if (!Directory.Exists(workingFolder))
            {
                Directory.CreateDirectory(workingFolder);
            }
            AnsiConsole.MarkupLine($"[green]Cloning {repository.Name} to {workingFolder}...[/]");
            TerminalHelper.Run(ConsoleMode.Powershell, $"git clone {repository.RemoteLocation} {workingFolder}");
        }

        public static void SyncUpSharedService(string hfPath)
        {
            var askToProceed = AnsiConsole.Ask<string>("All your hf-sharedservice changes will be lost. Do you want to proceed? (y/n)");
            if (askToProceed.ToLower().Equals("y"))
            {
                TerminalHelper.Run(ConsoleMode.Powershell, $"git reset --hard", Path.Combine(hfPath, "HFSharedService"));
                TerminalHelper.Run(ConsoleMode.Powershell, $"git fetch", Path.Combine(hfPath, "HFSharedService"));
                TerminalHelper.Run(ConsoleMode.Powershell, $"git checkout develop", Path.Combine(hfPath, "HFSharedService"));
                TerminalHelper.Run(ConsoleMode.Powershell, $"git pull", Path.Combine(hfPath, "HFSharedService"));
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Operation has been cancelled[/]");
            }
        }

        public static void CreateBranch(string hfPath, string issueId)
        {
            //Web Repo
            TerminalHelper.Run(ConsoleMode.Powershell, $"git branch {issueId}", hfPath);
            TerminalHelper.Run(ConsoleMode.Powershell, $"git checkout {issueId}", hfPath);
            TerminalHelper.Run(ConsoleMode.Powershell, $"git push --set-upstream origin {issueId}", hfPath);

            //Shared Service Repo
            TerminalHelper.Run(ConsoleMode.Powershell, $"git branch {issueId}", Path.Combine(hfPath, "HFSharedService"));
            TerminalHelper.Run(ConsoleMode.Powershell, $"git checkout {issueId}", Path.Combine(hfPath, "HFSharedService"));
            TerminalHelper.Run(ConsoleMode.Powershell, $"git push --set-upstream origin {issueId}", Path.Combine(hfPath, "HFSharedService"));
        }

        public static void GetLatest(string branchName, bool isClean)
        {

            var askToProceed = AnsiConsole.Ask<string>("All your local changes will be lost. Do you want to proceed? (y/n)");
            if (askToProceed.ToLower().Equals("y"))
            {
                var repositories = GetRepositories();

                foreach (var repo in repositories)
                {
                    var checkOutBranch = repo.DefaultBranch;

                    if (!string.IsNullOrEmpty(branchName))
                    {
                        checkOutBranch = branchName;
                    }

                    if (isClean)
                    {
                        TerminalHelper.Run(ConsoleMode.Powershell, $"git clean -fdx", repo.WorkingFolder);
                    }
                    else
                    {
                        TerminalHelper.Run(ConsoleMode.Powershell, $"git reset --hard", repo.WorkingFolder);
                    }

                    TerminalHelper.Run(ConsoleMode.Powershell, $"git fetch", repo.WorkingFolder);
                    TerminalHelper.Run(ConsoleMode.Powershell, $"git checkout {checkOutBranch}", repo.WorkingFolder);
                    TerminalHelper.Run(ConsoleMode.Powershell, $"git pull", repo.WorkingFolder);
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Operation has been cancelled[/]");
            }
        }


        public static List<Repository> GetRepositories()
        {
            var userStorage = UserStorageManager.GetUserStorage();
            return userStorage.Repositories;
        }

        public static void ConfigureSSH(string toolPath)
        {
            var userProfileFolder = Environment.GetEnvironmentVariable("USERPROFILE");
            var keyName = "id_rsa";
            var sshKeyFile = $"{userProfileFolder}\\.ssh\\{keyName}";
            var sshConfigFile = $"{userProfileFolder}\\.ssh\\Config";
            var knownHostsFile = $"{userProfileFolder}\\.ssh\\known_hosts";

            //Clean Existing SSH Key
            if (File.Exists(sshKeyFile))
            {
                File.Delete(sshKeyFile);
                File.Delete($"{sshKeyFile}.pub");
            }

            File.Copy($"{toolPath}\\Files\\SSH\\known_hosts", knownHostsFile, true);

            TerminalHelper.Run(ConsoleMode.Powershell, "Stop-Service ssh-agent");
            TerminalHelper.Run(ConsoleMode.Powershell, "Get-Service -Name ssh-agent | Select-Object -ExpandProperty Status");
            TerminalHelper.Run(ConsoleMode.Powershell, $"ssh-keygen -f \"{sshKeyFile}\"");
            TerminalHelper.Run(ConsoleMode.Powershell, "Start-Service ssh-agent");
            TerminalHelper.Run(ConsoleMode.Powershell, "Get-Service -Name ssh-agent | Select-Object -ExpandProperty Status");
            string publicKey = File.ReadAllText($"{sshKeyFile}.pub");
            ClipboardService.SetText(publicKey);
            AnsiConsole.MarkupLine($"[green]The public key has been copied to the clipboard. Add it to GitHub/BitBucket.[/]");


            //Create Config File
            if (!File.Exists(sshConfigFile))
            {
                string[] lines = { "Host bitbucket.org",
                                       "    AddKeysToAgent yes",
                                      $"    IdentityFile ~/.ssh/{keyName}",
                                       "Host github.com",
                                            "    AddKeysToAgent yes",
                                           $"    IdentityFile ~/.ssh/{keyName}"};

                File.WriteAllLines(sshConfigFile, lines);
            }

            //List all identities: ssh-add -L
            //Delete all identities: ssh-add -D
        }

        //internal static async Task RevertChanges(string revertTicketId, string branchName, string owner, string repositoryName)
        //{
        //    var repositories = GetRepositories();
        //    var repository = repositories.FirstOrDefault(r => r.Name.Equals(repositoryName));

        //    //Ensure there are no pending changes
        //    TerminalHelper.Run(ConsoleMode.Powershell, $"git reset --hard", repository.WorkingFolder);

        //    var gitHubConnector = new GitHubConnector();
        //    var commits = await gitHubConnector.GetCommits(owner, repository.Name, branchName);
        //    commits = commits.Where(c => c.commit.message.Contains(revertTicketId)).ToList();
        //    foreach (var commit in commits)
        //    {
        //        TerminalHelper.Run(ConsoleMode.Powershell, $"git revert {commit.sha}", repository.WorkingFolder);
        //    }

        //    GitHelper.SyncUpSharedService(repository.WorkingFolder);

        //    TerminalHelper.Run(ConsoleMode.Powershell, $"git push", repository.WorkingFolder);
        //}
    }
}