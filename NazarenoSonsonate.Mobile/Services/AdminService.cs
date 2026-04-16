using Microsoft.Maui.Storage;
using System;

namespace NazarenoSonsonate.Mobile.Services
{
    public class AdminService
    {
        private const string AdminKey = "is_admin";

        public bool IsAdmin { get; private set; }

        // 🔥 Evento para notificar cambios en tiempo real
        public event Action? OnChange;

        public AdminService()
        {
            // Cargar desde caché
            IsAdmin = Preferences.Get(AdminKey, false);
        }

        public bool ActivarAdmin(string clave)
        {
            if (clave == "nazareno2025") // 🔑 tu clave
            {
                IsAdmin = true;

                // 🔥 guardar en caché
                Preferences.Set(AdminKey, true);

                NotifyStateChanged(); // ⚡ notificar a toda la app
                return true;
            }
            return false;
        }

        public void DesactivarAdmin()
        {
            IsAdmin = false;

            // 🔥 limpiar caché
            Preferences.Remove(AdminKey);

            NotifyStateChanged(); // ⚡ notificar a toda la app
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}