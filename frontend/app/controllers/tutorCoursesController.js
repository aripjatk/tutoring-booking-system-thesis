(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('TutorCoursesController', ['apiService', 'authService', '$q', '$window', function (apiService, authService, $q, $window) {
      var vm = this;
      vm.user = authService.getSession();

      vm.state = { busy: true, error: '', saving: false };
      vm.courses = [];
      vm.search = '';

      vm.newCourse = {
        tutorUsername: vm.user.username,
        name: '',
        pricePerSession: 0,
        description: ''
      };

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        apiService.getCourses()
          .then(function (data) {
            vm.courses = (data || []).filter(c => !c.tutorUsername || c.tutorUsername === vm.user.username);
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to load courses.';
          })
          .finally(function () { vm.state.busy = false; });
      };

      vm.openDetail = function (id) {
        $window.location.hash = '#!/tutor/courses/' + id;
      };

      vm.create = function () {
        vm.state.saving = true; vm.state.error = '';
        var payload = angular.copy(vm.newCourse);
        payload.tutorUsername = vm.user.username;
        apiService.createCourse(payload)
          .then(function () {
            vm.newCourse.name = '';
            vm.newCourse.pricePerSession = 0;
            vm.newCourse.description = '';
            vm.load();
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to create course.';
          })
          .finally(function () { vm.state.saving = false; });
      };

      vm.delete = function (c) {
        if (!confirm('Delete course "' + (c.name || c.courseID) + '"?')) return;
        apiService.deleteCourse(c.courseID)
          .then(vm.load)
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to delete course.';
          });
      };

      vm.load();
    }]);
})();
