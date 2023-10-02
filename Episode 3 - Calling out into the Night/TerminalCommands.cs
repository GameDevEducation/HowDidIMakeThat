using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DevConsole.ConsoleCommand(new string[] {"status"})]
class StatusCommand
{
    public static string Help(string command, bool verbose)
    {
        if (verbose)
        {
            return command + System.Environment.NewLine +
                    "    Displays full system and shelter status.";
        }
        else
        {
            return "Displays full system and shelter status.";
        }
    }

    public static string Execute(string[] tokens)
    {
        return "<b>Emergency Radio</b>" + System.Environment.NewLine +
               "   Transmitter:    <color=#00ff00ff>Online</color>" + System.Environment.NewLine +
               "   Signal Scanner: <color=#00ff00ff>Online</color>" + System.Environment.NewLine + System.Environment.NewLine +
               "<b>Exterior Sensors</b>" + System.Environment.NewLine +
               "   Atmosphere:     <color=#ff0000ff>Hazardous</color>" + System.Environment.NewLine +
               "   Temperature:    <color=#ff0000ff>Online</color>" + System.Environment.NewLine + System.Environment.NewLine +
               "<b>Terminal</b>" + System.Environment.NewLine +
               "   Memory:         " + JournalManager.Instance.JournalsVisible + " journals using " + (JournalManager.Instance.JournalsVisible * 16) + " kb of memory" + System.Environment.NewLine;
    }

    public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
    {
        return null;
    }
}	

[DevConsole.ConsoleCommand(new string[] {"journal"})]
class JournalCommand
{
    public static string Help(string command, bool verbose)
    {
        if (verbose)
        {
            return command + " [journal number]" + System.Environment.NewLine +
                "    Displays a journal. The journal number must be between 1 and " + JournalManager.Instance.JournalsVisible + ".";
        }
        else
        {
            return "Displays a journal.";
        }
    }

    public static string Execute(string[] tokens)
    {
        if (tokens.Length == 0)
            return JournalCommand.Help("journal", true);

        int journalNumber = 0;
        if (int.TryParse(tokens[0], out journalNumber))
        {
            return JournalManager.Instance.GetJournal(journalNumber);            
        }

        return "<color=#ff0000ff>Journal number was not valid. It must be between 1 and " + JournalManager.Instance.JournalsVisible + ".";
    }

    public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
    {
        return null;
    }
}	

[DevConsole.ConsoleCommand(new string[] {"radio"})]
class RadioCommand
{
    public static string Help(string command, bool verbose)
    {
        if (verbose)
        {
            return command + System.Environment.NewLine +
                    "    [status] Shows the status of the emergency scan" + System.Environment.NewLine +
                    "    [scan]   Controls emergency radio system.";
        }
        else
        {
            return "Controls the emergency radio system.";
        }
    }

    public static string Execute(string[] tokens)
    {
        if (tokens.Length != 1)
            return RadioCommand.Help("radio", true);

        if (tokens[0] == "status")
        {
            return "<b>Emergency Message</b>" + System.Environment.NewLine +
                   "   <color=#00ff00ff>Transmitted on all channels</color>" + System.Environment.NewLine + System.Environment.NewLine +
                   "<b>Channel Scanning</b>" + System.Environment.NewLine +
                   "   " + GameManager.Instance.FrequencyBlocksScanned + " channels scanned for activity" + System.Environment.NewLine +
                   "   <color=#ff0000ff>No activity found</color>";
        }
        else if (tokens[0] == "scan")
        {
            // confirming radio scan?
            if (GameManager.Instance.RadioScanRequested)
            {
                GameManager.Instance.PerformRadioScan();
                DevConsole.ConsoleDaemon.Instance.OnExitConsole.Invoke();
                
                return "Beginning Scan";
            }
            else
            {
                GameManager.Instance.RadioScanRequested = true;

                return "<b>This will lock the terminal until scanning is complete.</b>" + System.Environment.NewLine + System.Environment.NewLine +
                       "Run the command again to begin scan.";
            }
        }

        return RadioCommand.Help("radio", true);
    }

    public static List<string> FetchAutocompleteOptions(string command, string[] tokens)
    {
        return null;
    }
}	
