namespace RobinBird.FirebaseTools
{
    using System;

    public class FirebaseAddressablesManager
    {
        private static bool isFirebaseSetupFinished;

        /// <summary>
        /// Set this bool as soon as the app is ready to download from Firebase Storage. If you require authentication
        /// to access items on Firebase Storage you should set this after your User has logged in.
        /// The Addressables Pipeline will wait and 'load' until you set this to true.
        /// </summary>
        public static bool IsFirebaseSetupFinished
        {
            get => isFirebaseSetupFinished;
            set
            {
                if (isFirebaseSetupFinished != value)
                {
                    isFirebaseSetupFinished = value;
                    FireFirebaseSetupFinished();
                }
            }
        }

        public static event Action FirebaseSetupFinished;

        private static void FireFirebaseSetupFinished()
        {
            FirebaseSetupFinished?.Invoke();
        }
    }
}