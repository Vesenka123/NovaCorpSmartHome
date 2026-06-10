using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaCorpSmartHome.BusinessLogic
{
    public static class OrderStatusValidator
    {
        /// <summary>
        /// Проверяет, допустим ли переход из текущего статуса в следующий.
        /// </summary>
        public static bool CanTransition(string currentStatus, string nextStatus, bool hasInstallationService)
        {
            if (string.IsNullOrEmpty(currentStatus) || string.IsNullOrEmpty(nextStatus))
                return false;

            // Пример правил перехода (адаптируйте под вашу логику)
            switch (currentStatus)
            {
                case "Оформлен":
                    return nextStatus == "Ожидает оплаты";

                case "Ожидает оплаты":
                    return nextStatus == "Оплачен";

                case "Оплачен":
                    // Если есть услуга установки, следующий статус - ожидает установки
                    if (hasInstallationService)
                        return nextStatus == "Ожидает установки";
                    else
                        return nextStatus == "Закрыт"; // Или другой финальный статус без установки

                case "Ожидает установки":
                    return nextStatus == "В работе"; // Менеджер назначает установщика

                case "В работе":
                    return nextStatus == "Закрыт"; // Установщик завершает работу

                default:
                    return false;
            }
        }

        public static bool IsInstallerAllowed(int currentInstallerId, int orderInstallerId)
        {
            return currentInstallerId == orderInstallerId;
        }
    }
}