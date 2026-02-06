namespace TutorApp.API {
    public class ErrorMessages {
        public readonly static string UserNotFound = "Logged-in user not found in database - either the account was deleted or the DB is corrupted";
        public readonly static string NotATutor = "This operation is only permitted for tutors";
        public readonly static string NotAStudent = "This operation is only permitted for students";
        public readonly static string UsernameTaken = "Username is taken";
        public readonly static string EmailTaken = "There is already an account registered with the given email address";
    }
}
