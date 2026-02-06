(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('TutorDashboardController', ['apiService', 'authService', '$q', function (apiService, authService, $q) {
      var vm = this;
      vm.user = authService.getSession();
      vm.state = { busy: true, error: '' };

      vm.courses = [];
      vm.sessions = [];
      vm.students = [];

      vm.refresh = function () {
        vm.state.busy = true;
        vm.state.error = '';

        $q.all([apiService.getCourses(), apiService.getSessions(), apiService.getAccounts()])
          .then(function (values) {
            vm.courses = (values[0] || []).filter(c => !c.tutorUsername || c.tutorUsername === vm.user.username);
            vm.sessions = values[1] || [];
            var accounts = values[2] || [];
            vm.students = accounts.filter(a => a && a.isTutor === false);

            vm.sessions.forEach(function (s) {
              if (s.sessionDateTime && !s.sessionDateTime.endsWith('Z')) s.sessionDateTime += 'Z';
              var match = vm.courses.find(function (c) { return c.courseID === s.courseID; });
              if (match) s.course = match;
            });

            var now = new Date();
            var max = new Date(now.getTime() + 14*24*60*60*1000);
            vm.upcoming = (vm.sessions || [])
              .map(function (s) { s._dt = s.sessionDateTime ? new Date(s.sessionDateTime) : null; return s; })
              .filter(function (s) { return s._dt && !isNaN(s._dt) && s._dt >= now && s._dt <= max; })
              .sort(function (a,b) { return a._dt - b._dt; })
              .slice(0, 10);

          })
          .catch(function (err) {
            vm.state.error = (err && err.data) ? err.data : (err && err.message ? err.message : 'Failed to load dashboard data.');
          })
          .finally(function () { vm.state.busy = false; });
      };

      vm.refresh();
    }]);
})();
