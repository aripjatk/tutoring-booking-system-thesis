(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('StudentCoursesController', ['apiService', '$q', function (apiService, $q) {
      var vm = this;
      vm.state = { busy: true, error: '' };
      vm.myCourses = [];
      vm.search = '';

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        apiService.getMyCourses()
          .then(function (data) {
            vm.myCourses = data || [];
            var promises = vm.myCourses.map(function (sc) {
              return apiService.getCourse(sc.courseID)
                .then(function (course) { sc.course = course; });
            });
            return $q.all(promises);
          })
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to load courses.'; })
          .finally(function () { vm.state.busy = false; });
      };

      vm.load();
    }]);
})();
