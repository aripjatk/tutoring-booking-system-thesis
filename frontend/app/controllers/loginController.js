(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('LoginController', ['authService', 'apiService', '$window', 'APP_CONFIG', function (authService, apiService, $window, APP_CONFIG) {
      var vm = this;
      vm.apiBaseUrl = APP_CONFIG.apiBaseUrl;
      vm.form = { username: '', password: '' };
      vm.state = { busy: false, error: '' };

      vm.isRegistering = false;
      vm.registerForm = { username: '', displayName: '', email: '', password: '' };

      vm.toggleRegister = function () {
        vm.isRegistering = !vm.isRegistering;
        vm.state.error = '';
        vm.state.busy = false;
      };

      vm.register = function () {
        vm.state.error = '';
        vm.state.busy = true;

        apiService.registerTutor(vm.registerForm)
          .then(function () {
            $window.location.hash = '#!/registration-pending';
          })
          .catch(function (err) {
            var msg = 'Registration failed.';
            if (err && err.data) msg = err.data;
            if (err && err.message) msg = err.message;
            vm.state.error = msg;
          })
          .finally(function () { vm.state.busy = false; });
      };

      vm.login = function () {
        vm.state.error = '';
        vm.state.busy = true;

        authService.login(vm.form.username, vm.form.password)
          .then(function (session) {
            if (session.isTutor) $window.location.hash = '#!/tutor/dashboard';
            else $window.location.hash = '#!/student/dashboard';
          })
          .catch(function (err) {
            var msg = 'Login failed.';
            if (err && err.data) msg = err.data;
            if (err && err.message) msg = err.message;
            vm.state.error = msg;
          })
          .finally(function () { vm.state.busy = false; });
      };
    }]);
})();
