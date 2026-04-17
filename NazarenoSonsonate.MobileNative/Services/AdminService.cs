using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.MobileNative.Services
{
    public class AdminService
    {
        private const string AdminKey = "is_admin";

        public bool IsAdmin { get; private set; }

        public event Action? OnChange;

        public AdminService()
        {
            IsAdmin = Preferences.Get(AdminKey, false);
        }

        public bool ActivarAdmin(string clave)
        {
            if (clave == "nazareno2025")
            {
                IsAdmin = true;
                Preferences.Set(AdminKey, true);
                NotifyStateChanged();
                return true;
            }

            return false;
        }

        public void DesactivarAdmin()
        {
            IsAdmin = false;
            Preferences.Remove(AdminKey);
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}
