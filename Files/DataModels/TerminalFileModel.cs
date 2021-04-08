﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.DataModels
{
    public class TerminalFileModel
    {
        [JsonProperty("DefaultTerminalName")]
        public string DefaultTerminalName { get; set; }

        [JsonProperty("terminals")]
        public List<Terminal> Terminals { get; set; } = new List<Terminal>();

        [JsonProperty("version")]
        public int Version { get; set; }

        public void AddTerminal(Terminal terminal)
        {
            //Ensure terminal is not already in List
            if (Terminals.FirstOrDefault(x => x.Name.Equals(terminal.Name, StringComparison.OrdinalIgnoreCase)) == null)
            {
                Terminals.Add(terminal);
            }
        }

        public Terminal GetDefaultTerminal()
        {
            Terminal terminal = Terminals.FirstOrDefault(x => x.Name.Equals(DefaultTerminalName, StringComparison.OrdinalIgnoreCase));
            if (terminal != null)
            {
                return terminal;
            }
            else
            {
                ResetToDefaultTerminal();
            }

            return Terminals.First();
        }

        public void RemoveTerminal(Terminal terminal)
        {
            if (Terminals.Remove(Terminals.FirstOrDefault(x => x.Name.Equals(terminal.Name, StringComparison.OrdinalIgnoreCase))))
            {
                if (string.IsNullOrWhiteSpace(DefaultTerminalName))
                {
                    ResetToDefaultTerminal();
                }
                else if (DefaultTerminalName.Equals(terminal.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ResetToDefaultTerminal();
                }
            }
        }

        public void ResetToDefaultTerminal()
        {
            DefaultTerminalName = "cmd";
        }
    }
}