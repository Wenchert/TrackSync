using TrackSync.DataBase.Model;

namespace TrackSync.Functions
{

    /// Управление сессией текущего пользователя
    public static class SessionManager
    {
        private static User? _currentUser;


        /// Текущий авторизованный пользователь

        public static User? CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                CurrentUserId = value?.UserId ?? 0;
            }
        }


        /// ID текущего пользователя
        public static int CurrentUserId { get; private set; }

        /// Очистка сессии (выход из системы)
        public static void ClearSession()
        {
            _currentUser = null;
            CurrentUserId = -1;
        }
    }
}