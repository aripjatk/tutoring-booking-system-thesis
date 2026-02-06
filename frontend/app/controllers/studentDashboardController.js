(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('StudentDashboardController', ['apiService', 'authService', '$q', function (apiService, authService, $q) {
      var vm = this;
      vm.user = authService.getSession();
      vm.state = { busy: true, error: '' };

      vm.myCourses = [];
      vm.mySessions = [];
      vm.upcoming = [];

      vm.refresh = function () {
        vm.state.busy = true; vm.state.error = '';

        $q.all([apiService.getMyCourses(), apiService.getMySessions()])
          .then(function (values) {
            vm.myCourses = values[0] || [];
            vm.mySessions = values[1] || [];

            vm.mySessions.forEach(function (s) {
              if (s.sessionDateTime && !s.sessionDateTime.endsWith('Z')) s.sessionDateTime += 'Z';
            });

            var ids = [];
            vm.myCourses.forEach(function (c) { ids.push(c.courseID); });
            vm.mySessions.forEach(function (s) { ids.push(s.courseID); });
            ids = ids.filter(function (v, i, a) { return a.indexOf(v) === i; });

            var promises = ids.map(function (id) {
              return apiService.getCourse(id).catch(function () { return null; });
            });

            return $q.all(promises).then(function (courses) {
              var map = {};
              courses.forEach(function (c) { if (c) map[c.courseID] = c; });

              vm.myCourses.forEach(function (mc) {
                if (map[mc.courseID]) mc.course = map[mc.courseID];
              });

              vm.mySessions.forEach(function (s) {
                if (map[s.courseID]) s.course = map[s.courseID];
              });

              var now = new Date();
              var max = new Date(now.getTime() + 14 * 24 * 60 * 60 * 1000);

              vm.upcoming = (vm.mySessions || [])
                .map(function (s) {
                  s._dt = s.sessionDateTime ? new Date(s.sessionDateTime) : null;
                  var statusMap = { 0: 'Unknown', 1: 'Confirmed', 2: 'Rejected' };
                  s.confirmationStatusLabel = statusMap[s.confirmationStatus] || 'Unknown';
                  return s;
                })
                .filter(function (s) { return s._dt && !isNaN(s._dt) && s._dt >= now && s._dt <= max; })
                .sort(function (a, b) { return a._dt - b._dt; })
                .slice(0, 10);
            });
          })
          .catch(function (err) {
            vm.state.error = (err && err.data) ? err.data : (err && err.message ? err.message : 'Failed to load dashboard.');
          })
          .finally(function () { vm.state.busy = false; });
      };

      vm.refresh();
    }]);
})();
