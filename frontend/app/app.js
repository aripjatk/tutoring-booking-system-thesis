(function () {
  'use strict';

  angular.module('tbsApp', ['ngRoute'])
    .constant('APP_CONFIG', {
      apiBaseUrl: window.TBS_CONFIG.apiBaseUrl
    })
    .config(['$routeProvider', '$httpProvider', function ($routeProvider, $httpProvider) {

      $routeProvider
        .when('/login', {
          templateUrl: 'app/views/login.html',
          controller: 'LoginController',
          controllerAs: 'vm'
        })
        .when('/registration-pending', {
          templateUrl: 'app/views/registration-pending.html'
        })
        .when('/', {
          template: '<div class="text-secondary">Redirecting…</div>',
          resolve: {
            go: ['authService', '$window', function (authService, $window) {
              if (!authService.isAuthenticated()) { $window.location.hash = '#!/login'; return true; }
              if (authService.isTutor()) { $window.location.hash = '#!/tutor/dashboard'; return true; }
              $window.location.hash = '#!/student/dashboard'; return true;
            }]
          }
        })

        .when('/tutor/dashboard', {
          templateUrl: 'app/views/tutor/dashboard.html',
          controller: 'TutorDashboardController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('tutor'); }] }
        })
        .when('/tutor/notes', {
          templateUrl: 'app/views/notes.html',
          controller: 'NotesController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('tutor'); }] }
        })
        .when('/tutor/courses', {
          templateUrl: 'app/views/tutor/courses.html',
          controller: 'TutorCoursesController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('tutor'); }] }
        })
        .when('/tutor/courses/:id', {
          templateUrl: 'app/views/tutor/course-detail.html',
          controller: 'TutorCourseDetailController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('tutor'); }] }
        })
        .when('/tutor/students', {
          templateUrl: 'app/views/tutor/students.html',
          controller: 'TutorStudentsController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('tutor'); }] }
        })
        .when('/tutor/students/new', {
          templateUrl: 'app/views/tutor/student-create.html',
          controller: 'TutorStudentCreateController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('tutor'); }] }
        })
        .when('/tutor/sessions', {
          templateUrl: 'app/views/tutor/sessions.html',
          controller: 'TutorSessionsController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('tutor'); }] }
        })
        .when('/tutor/sessions/:id', {
          templateUrl: 'app/views/tutor/session-detail.html',
          controller: 'TutorSessionDetailController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('tutor'); }] }
        })
        .when('/tutor/payments', {
          templateUrl: 'app/views/tutor/payments.html',
          controller: 'TutorPaymentsController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('tutor'); }] }
        })

        .when('/student/dashboard', {
          templateUrl: 'app/views/student/dashboard.html',
          controller: 'StudentDashboardController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('student'); }] }
        })
        .when('/student/notes', {
          templateUrl: 'app/views/notes.html',
          controller: 'NotesController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('student'); }] }
        })
        .when('/student/courses', {
          templateUrl: 'app/views/student/courses.html',
          controller: 'StudentCoursesController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('student'); }] }
        })
        .when('/student/courses/:id', {
          templateUrl: 'app/views/student/course-detail.html',
          controller: 'StudentCourseDetailController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('student'); }] }
        })
        .when('/student/sessions', {
          templateUrl: 'app/views/student/sessions.html',
          controller: 'StudentSessionsController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('student'); }] }
        })
        .when('/student/homework', {
          templateUrl: 'app/views/student/homework.html',
          controller: 'StudentHomeworkController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('student'); }] }
        })
        .when('/student/homework/:id', {
          templateUrl: 'app/views/student/homework-detail.html',
          controller: 'StudentHomeworkDetailController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('student'); }] }
        })
        .when('/student/payments', {
          templateUrl: 'app/views/student/payments.html',
          controller: 'StudentPaymentsController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth('student'); }] }
        })

        .when('/messages', {
          templateUrl: 'app/views/messages.html',
          controller: 'MessagesController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth(); }] }
        })
        .when('/messages/sent', {
          templateUrl: 'app/views/messages.html',
          controller: 'MessagesController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth(); }] }
        })
        .when('/messages/new', {
          templateUrl: 'app/views/message-compose.html',
          controller: 'MessageComposeController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth(); }] }
        })
        .when('/messages/:id', {
          templateUrl: 'app/views/message-detail.html',
          controller: 'MessageDetailController',
          controllerAs: 'vm',
          resolve: { auth: ['authService', function (authService) { return authService.requireAuth(); }] }
        })

        .when('/notifications', {
          templateUrl: 'app/views/notifications.html',
          controller: 'NotificationsController',
          controllerAs: 'vm'
        })

        .otherwise({ redirectTo: '/login' });

      $httpProvider.interceptors.push(['$q', '$injector', function ($q, $injector) {
        return {
          request: function (config) {
            var authService = $injector.get('authService');
            var token = authService.getToken();
            if (token) {
              config.headers = config.headers || {};
              config.headers.Authorization = 'Bearer ' + token;
            }
            return config;
          },
          responseError: function (rejection) {
            if (rejection && (rejection.status === 401 || rejection.status === 403)) {
              var authService = $injector.get('authService');
              authService.clearSession();
              window.location.hash = '#!/login';
            }
            return $q.reject(rejection);
          }
        };
      }]);

    }]);
})();
