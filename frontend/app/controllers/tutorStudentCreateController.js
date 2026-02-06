(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('TutorStudentCreateController', ['apiService', '$window', function (apiService, $window) {
      var vm = this;

      vm.state = { saving: false, error: '' };

      vm.form = {
        username: '',
        displayName: '',
        email: '',
        password: ''
      };

      vm.save = function () {
        vm.state.saving = true; vm.state.error = '';
        apiService.registerStudent(vm.form)
          .then(function () {
            alert('Student registration submitted. Student must verify email to activate the account.');
            $window.location.hash = '#!/tutor/students';
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to register student.';
          })
          .finally(function () { vm.state.saving = false; });
      };

      vm.cancel = function () { $window.location.hash = '#!/tutor/students'; };
    }]);
})();
