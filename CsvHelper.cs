using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace HVLauncher
{
    public static class CsvHelper
    {
        public static List<CsvRow> ReadCsv(string path)
        {
            var rows = new List<CsvRow>();
            if (!File.Exists(path)) return rows;

            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var columns = line.Split(',').Select(s => s.Trim('"')).ToArray();
                if (columns.Length == 6)
                {
                    rows.Add(new CsvRow
                    {
                        GameName = columns[0],
                        IconPath = columns[1],
                        GamePath = columns[2],
                        HVPath = columns[3],
                        filename = columns[4],
                        LaunchService = bool.Parse(columns[5])
                    });
                }
            }
            return rows;
        }
        public static void SaveCsv(string path, List<CsvRow> rows)
        {
            var lines = rows.Select(r => "\"" + r.GameName + "\",\"" + r.IconPath + "\",\"" + r.GamePath + "\",\"" + r.HVPath + "\",\"" + r.filename + "\",\"" + r.LaunchService + "\"");
            File.WriteAllLines(path, lines);
        }
        public static void AddRow(string path, CsvRow row)
        {
            var rows = ReadCsv(path);
            rows.Add(row);
            SaveCsv(path, rows);
        }
        public static void EditRow(string path, int index, CsvRow newRow)
        {
            var rows = ReadCsv(path);
            if (index >= 0 && index < rows.Count)
            {
                rows[index] = newRow;
                SaveCsv(path, rows);
            }
        }
        public static void DeleteRow(string path, int index)
        {
            var rows = ReadCsv(path);
            if (index >= 0 && index < rows.Count)
            {
                rows.RemoveAt(index);
                SaveCsv(path, rows);
            }
        }

    }
}
