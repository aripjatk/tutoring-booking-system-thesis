(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('StudentHomeworkController', ['apiService', '$window', function (apiService, $window) {
      var vm = this;
      vm.state = { busy: true, error: '' };
      vm.assignments = [];

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        apiService.getHomeworkAssignments()
          .then(function (data) { vm.assignments = data || []; })
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to load homework.'; })
          .finally(function () { vm.state.busy = false; });
      };

      vm.goToDetail = function (id) {
          $window.location.hash = '#!/student/homework/' + id;
      };

      vm.load();
    }]);
})();
