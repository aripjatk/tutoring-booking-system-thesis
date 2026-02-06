(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('AppController', ['authService', 'apiService', '$window', '$rootScope', function (authService, apiService, $window, $rootScope) {
      var vm = this;

      vm.theme = loadTheme();
      vm.notificationCount = 0;

      function refreshUser() {
        vm.currentUser = authService.getSession() ? { username: authService.getSession().username } : {};
      }
      refreshUser();

      vm.isAuthenticated = function () { return authService.isAuthenticated(); };
      vm.isTutor = function () { return authService.isTutor(); };
      vm.isStudent = function () { return authService.isStudent(); };

      vm.loadNotifications = function () {
        if (vm.isAuthenticated()) {
          apiService.getNotifications()
            .then(function (data) {
              vm.notificationCount = (data || []).length;
            })
            .catch(function () {});
        } else {
          vm.notificationCount = 0;
        }
      };

      vm.logout = function () {
        authService.logout();
        refreshUser();
        vm.notificationCount = 0;
        $window.location.hash = '#!/login';
      };

      vm.toggleTheme = function () {
        vm.theme = (vm.theme === 'theme-dark') ? 'theme-light' : 'theme-dark';
        localStorage.setItem('tbs_theme', vm.theme);
      };

      $rootScope.$on('$routeChangeSuccess', function () {
        refreshUser();
        vm.loadNotifications();
      });

      $rootScope.$on('notificationsUpdated', function () {
        vm.loadNotifications();
      });

      vm.loadNotifications();

      function loadTheme() {
        var t = localStorage.getItem('tbs_theme');
        if (t === 'theme-dark' || t === 'theme-light') return t;
        return (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) ? 'theme-dark' : 'theme-light';
      }
    }]);
})();
