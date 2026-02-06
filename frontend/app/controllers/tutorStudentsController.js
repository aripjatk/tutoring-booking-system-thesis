(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('TutorStudentsController', ['apiService', '$window', function (apiService, $window) {
      var vm = this;
      vm.state = { busy: true, error: '' };
      vm.students = [];
      vm.search = '';

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        apiService.getAccounts()
          .then(function (accounts) {
            vm.students = (accounts || []).filter(a => a && a.isTutor === false);
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to load students.';
          })
          .finally(function () { vm.state.busy = false; });
      };

      vm.create = function () { $window.location.hash = '#!/tutor/students/new'; };

      vm.message = function (s) {
        $window.location.hash = '#!/messages/new?recipient=' + encodeURIComponent(s.username);
      };

      vm.deactivate = function (s) {
        if (!confirm('Deactivate student "' + s.username + '"?')) return;
        apiService.deactivateAccount(s.username)
          .then(vm.load)
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to deactivate.'; });
      };

      vm.load();
    }]);
})();
