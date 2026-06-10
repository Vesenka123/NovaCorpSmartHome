using Microsoft.VisualStudio.TestTools.UnitTesting;
using NovaCorpSmartHome.BusinessLogic;
using System;

namespace NovaCorpSmartHome.Tests
{
    [TestClass]
    public class OrderStatusTests
    {
        [TestMethod]
        public void Transition_FromCreatedToWaitingPayment_ReturnsTrue()
        {
            // Arrange
            string currentStatus = "Оформлен";
            string nextStatus = "Ожидает оплаты";
            bool hasInstallation = true;

            // Act
            bool result = OrderStatusValidator.CanTransition(currentStatus, nextStatus, hasInstallation);

            // Assert
            Assert.IsTrue(result, "Переход из 'Оформлен' в 'Ожидает оплаты' должен быть разрешен.");
        }

        [TestMethod]
        public void Transition_FromPaidToWaitingInstallation_WithService_ReturnsTrue()
        {
            // Arrange
            string currentStatus = "Оплачен";
            string nextStatus = "Ожидает установки";
            bool hasInstallation = true;

            // Act
            bool result = OrderStatusValidator.CanTransition(currentStatus, nextStatus, hasInstallation);

            // Assert
            Assert.IsTrue(result, "При наличии услуги установки статус должен перейти в 'Ожидает установки'.");
        }

        [TestMethod]
        public void Transition_FromPaidToClosed_WithoutService_ReturnsTrue()
        {
            // Arrange
            string currentStatus = "Оплачен";
            string nextStatus = "Закрыт";
            bool hasInstallation = false;

            // Act
            bool result = OrderStatusValidator.CanTransition(currentStatus, nextStatus, hasInstallation);

            // Assert
            Assert.IsTrue(result, "Без услуги установки заказ должен закрыться сразу после оплаты.");
        }

        [TestMethod]
        public void Transition_InvalidStatusChange_ReturnsFalse()
        {
            // Arrange
            string currentStatus = "Оформлен";
            string nextStatus = "Закрыт"; // Нельзя сразу закрыть неоформленный/неоплаченный заказ
            bool hasInstallation = true;

            // Act
            bool result = OrderStatusValidator.CanTransition(currentStatus, nextStatus, hasInstallation);

            // Assert
            Assert.IsFalse(result, "Прямой переход из 'Оформлен' в 'Закрыт' запрещен.");
        }

        [TestMethod]
        public void CheckInstallerAccess_MatchingId_ReturnsTrue()
        {
            // Arrange & Act
            bool result = OrderStatusValidator.IsInstallerAllowed(5, 5);

            // Assert
            Assert.IsTrue(result, "Установщик должен иметь доступ к своему заказу.");
        }

        [TestMethod]
        public void CheckInstallerAccess_DifferentId_ReturnsFalse()
        {
            // Arrange & Act
            bool result = OrderStatusValidator.IsInstallerAllowed(5, 10);

            // Assert
            Assert.IsFalse(result, "Установщик не должен иметь доступ к чужому заказу.");
        }
    }
}