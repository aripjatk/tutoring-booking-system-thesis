(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('StudentCourseDetailController', ['apiService', '$routeParams', '$window', '$q', function (apiService, $routeParams, $window, $q) {
      var vm = this;
      vm.courseId = parseInt($routeParams.id, 10);
      vm.state = { busy: true, error: '' };
      vm.course = null;
      vm.teachingMaterials = [];

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        $q.all([
          apiService.getCourse(vm.courseId),
          apiService.getTeachingMaterials()
        ]).then(function (values) {
          vm.course = values[0];
          var allMaterials = values[1] || [];
          vm.teachingMaterials = allMaterials.filter(function(m) {
              return m.courseID === vm.courseId;
          });
        }).catch(function (err) {
          vm.state.error = (err && err.data) ? err.data : 'Failed to load course details.';
        }).finally(function () { vm.state.busy = false; });
      };

      vm.downloadMaterial = function (material) {
        apiService.getTeachingMaterialFile(material.teachingMaterialID)
          .then(function (response) {
             var blob = response.data;
             var link = document.createElement('a');
             link.href = window.URL.createObjectURL(blob);
             link.download = material.fileName || 'material';
             link.click();
             window.URL.revokeObjectURL(link.href);
          })
          .catch(function () { alert('Failed to download file.'); });
      };

      vm.back = function () {
          $window.location.hash = '#!/student/courses';
      };

      vm.load();
    }]);
})();
