using TrackSync.DataBase.Model;

namespace TrackSync.Functions
{

    public static class SessionManager
    {
        private static User? _currentUser;

        public static User? CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                CurrentUserId = value?.UserId ?? 0;
            }
        }

        public static int CurrentUserId { get; private set; }

        public static void ClearSession()
        {
            _currentUser = null;
            CurrentUserId = -1;
        }
    }
}