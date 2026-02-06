(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('StudentHomeworkDetailController', ['apiService', '$routeParams', '$window', function (apiService, $routeParams, $window) {
      var vm = this;
      vm.id = $routeParams.id;
      vm.state = { busy: true, error: '', success: '' };
      vm.assignment = null;

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        apiService.getHomeworkAssignment(vm.id)
          .then(function (data) { vm.assignment = data; })
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to load assignment.'; })
          .finally(function () { vm.state.busy = false; });
      };

      vm.upload = function () {
        var fileInput = document.getElementById('solutionFile');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            alert('Please select a file first.');
            return;
        }
        var file = fileInput.files[0];

        vm.state.busy = true; vm.state.error = ''; vm.state.success = '';
        apiService.uploadHomeworkSolution(vm.id, file)
          .then(function () {
             vm.state.success = 'Solution uploaded successfully.';
             vm.load();
          })
          .catch(function (err) {
             vm.state.error = err && err.data ? err.data : 'Failed to upload solution.';
          })
          .finally(function () { vm.state.busy = false; });
      };

      vm.downloadFile = function () {
         if (!vm.assignment || !vm.assignment.solutionFileName) return;
         apiService.downloadHomeworkFile(vm.id)
           .then(function (response) {
               var blob = response.data;
               var link = document.createElement('a');
               link.href = window.URL.createObjectURL(blob);
               link.download = vm.assignment.solutionFileName;
               link.click();
               window.URL.revokeObjectURL(link.href);
           })
           .catch(function () { alert('Failed to download file.'); });
      };

      vm.back = function () {
          $window.location.hash = '#!/student/homework';
      };

      vm.load();
    }]);
})();
