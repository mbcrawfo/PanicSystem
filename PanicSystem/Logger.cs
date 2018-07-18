﻿using System;
using System.IO;
using static PanicSystem.PanicSystem;

namespace PanicSystem
{
    // code 'borrowed' from Morphyum
    public static class Logger
    {
        static string _filePath = $"{ModDirectory}/Log.txt";
        public static void LogError(Exception ex)
        {
            using (var writer = new StreamWriter(_filePath, true))
            {
                writer.WriteLine($"{DateTime.Now.ToShortTimeString()} Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
                writer.WriteLine(new string(c: '-', count: 80));
            }
        }

        public static void Debug(object line)
        {
            // idea 'borrowed' from jo
            if (!PanicSystem.Settings.DebugEnabled) return;
            using (var writer = new StreamWriter(_filePath, true))
            {
                writer.WriteLine($"{DateTime.Now.ToShortTimeString()} {line}");
            }
        }
    }
}