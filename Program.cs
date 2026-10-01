using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        systemInformation();
    }

    static void systemInformation()
    {
        while (true)
        {
            string input = showMenu();

            if (!int.TryParse(input, out int choice))
            {
                Console.WriteLine("Ошибка: введите число!");
                continue;
            }

            switch (choice)
            {
                case 1:
                    showSystemInfo();
                    break;
                case 2:
                    showTaskList();
                    break;
                case 3:
                    showFullStatistics();
                    break;
                case 4:
                    Console.WriteLine("Выход...");
                    return;
                default:
                    Console.WriteLine("Нет такого пункта меню");
                    break;
            }

            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }
    }

    static string showMenu()
    {
        Console.Clear();
        Console.WriteLine("===== МОНИТОР СИСТЕМЫ =====");
        Console.WriteLine("1. Основная информация о системе");
        Console.WriteLine("2. Список задач (память)");
        Console.WriteLine("3. Полная статистика загруженности");
        Console.WriteLine("4. Выход");
        Console.Write("Выберите пункт: ");
        return Console.ReadLine();
    }

    static void showSystemInfo()
    {
        Console.WriteLine($"ОС: {Environment.OSVersion}");
        Console.WriteLine($"Машина: {Environment.MachineName}");
        Console.WriteLine($"Пользователь: {Environment.UserName}");
        Console.WriteLine($"Процессоров: {Environment.ProcessorCount}");
        Console.WriteLine($"64-битная ОС: {Environment.Is64BitOperatingSystem}");
        Console.WriteLine($"Версия .NET: {Environment.Version}");
        Console.WriteLine($"Время работы системы: {getUptime()}");
    }

    static TimeSpan getUptime()
    {
        try
        {
            using var pc = new PerformanceCounter("System", "System Up Time");
            pc.NextValue(); // первый вызов всегда возвращает 0
            return TimeSpan.FromSeconds(pc.NextValue());
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }
    static void showTaskList()
    {
        Console.WriteLine($"{"ID",-8} {"Память (МБ)",-15} {"Имя"}");
        Console.WriteLine(new string('-', 45));

        var processes = Process.GetProcesses()
            .OrderByDescending(p => SafeGetMemory(p)); // без Take(30)

        int shown = 0, skipped = 0;

        foreach (var process in processes)
        {
            double memoryMb = SafeGetMemory(process) / 1024.0 / 1024.0;

            if (memoryMb < 0)
            {
                skipped++; // нет доступа
            }
            else
            {
                Console.WriteLine($"{process.Id,-8} {memoryMb,-15:F1} {process.ProcessName}");
                shown++;
            }

            process.Dispose();
        }

        Console.WriteLine(new string('-', 45));
        Console.WriteLine($"Показано: {shown}, пропущено (нет доступа): {skipped}");
    }

    static long SafeGetMemory(Process p)
    {
        try
        {
            return p.WorkingSet64;
        }
        catch
        {
            return -1;
        }
    }

    static void showFullStatistics()
    {
        Console.WriteLine("===== ЗАГРУЖЕННОСТЬ СИСТЕМЫ =====\n");

        Console.WriteLine($"--- Процессор ({Environment.ProcessorCount} ядер) ---");
        try
        {
            using var cpuTotal = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuTotal.NextValue();
            System.Threading.Thread.Sleep(500); // PerformanceCounter требует паузу между вызовами
            Console.WriteLine($"Общая загрузка CPU: {cpuTotal.NextValue():F1}%");
        }
        catch { Console.WriteLine("CPU: недоступно (нужны права администратора?)"); }

        Console.WriteLine("\n--- Оперативная память ---");
        var ramInfo = getRamInfo();
        Console.WriteLine($"Всего: {ramInfo.total:F1} МБ");
        Console.WriteLine($"Занято: {ramInfo.used:F1} МБ ({ramInfo.percent:F1}%)");
        Console.WriteLine($"Свободно: {ramInfo.free:F1} МБ");
        drawBar(ramInfo.percent);

        Console.WriteLine("\n--- Диски ---");
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;

            double totalGb = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
            double freeGb = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
            double usedPercent = (1 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100;

            Console.WriteLine($"{drive.Name} ({drive.VolumeLabel})");
            Console.WriteLine($"  {totalGb - freeGb:F1} / {totalGb:F1} ГБ занято ({usedPercent:F1}%)");
            drawBar(usedPercent);
        }

        Console.WriteLine("\n--- Топ-5 процессов по памяти ---");
        Process.GetProcesses()
            .OrderByDescending(p => p.WorkingSet64)
            .Take(5)
            .ToList()
            .ForEach(p =>
            {
                try
                {
                    Console.WriteLine($"  {p.ProcessName,-25} {p.WorkingSet64 / 1024.0 / 1024.0:F1} МБ");
                }
                catch { }
                finally { p.Dispose(); }
            });
    }

    static double getCpuUsage(Process process)
    {
        try
        {
            return 0; 
        }
        catch { return -1; }
    }

    static (double total, double free, double used, double percent) getRamInfo()
    {
        try
        {
            double totalMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0;

            using var availPc = new PerformanceCounter("Memory", "Available MBytes");
            availPc.NextValue();
            System.Threading.Thread.Sleep(200);
            double freeMb = availPc.NextValue();

            double usedMb = totalMb - freeMb;
            double percent = usedMb / totalMb * 100;

            return (totalMb, freeMb, usedMb, percent);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    static void drawBar(double percent)
    {
        int width = 30;
        int filled = (int)(percent / 100 * width);
        string bar = new string('#', filled) + new string('-', width - filled);
        Console.WriteLine($"  [{bar}] {percent:F0}%");
    }
}