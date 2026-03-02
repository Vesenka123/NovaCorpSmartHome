using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaCorpSmartHome.ApplicationData
{
    public static class AppEvents
    {
        // Событие для обновления статистики
        public static event EventHandler StatisticsChanged;

        // Метод для вызова события
        public static void OnStatisticsChanged()
        {
            StatisticsChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}