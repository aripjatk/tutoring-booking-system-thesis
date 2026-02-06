(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('StudentSessionsController', ['apiService', '$q', function (apiService, $q) {
      var vm = this;
      vm.state = { busy: true, error: '' };
      vm.sessions = [];
      vm.filter = { from: '', to: '' };

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        apiService.getMySessions()
          .then(function (data) {
            vm.sessions = data || [];

            vm.sessions.forEach(function (s) {
              if (s.sessionDateTime && !s.sessionDateTime.endsWith('Z')) s.sessionDateTime += 'Z';
            });

            var courseIds = vm.sessions
              .map(function (s) { return s.courseID; })
              .filter(function (v, i, a) { return a.indexOf(v) === i; });

            var promises = courseIds.map(function (id) {
              return apiService.getCourse(id).catch(function () { return null; });
            });

            return $q.all(promises).then(function (courses) {
              var courseMap = {};
              courses.forEach(function (c) { if (c) courseMap[c.courseID] = c; });

              vm.sessions.forEach(function (s) {
                if (courseMap[s.courseID]) s.course = courseMap[s.courseID];
              });
            });
          })
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to load sessions.'; })
          .finally(function () { vm.state.busy = false; });
      };

      vm.confirm = function (s) {
        if (!confirm('Confirm this session?')) return;
        vm.state.busy = true;
        apiService.acceptSession(s.sessionID)
          .then(vm.load)
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to confirm session.'; vm.state.busy = false; });
      };

      vm.reject = function (s) {
        if (!confirm('Reject this session?')) return;
        vm.state.busy = true;
        apiService.rejectSession(s.sessionID)
          .then(vm.load)
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to reject session.'; vm.state.busy = false; });
      };

      vm.getStatusLabel = function (status) {
        switch (status) {
          case 0: return 'Unknown';
          case 1: return 'Confirmed';
          case 2: return 'Rejected';
          default: return status;
        }
      };

      vm.filtered = function () {
        var list = vm.sessions || [];
        var from = vm.filter.from ? new Date(vm.filter.from + 'T00:00:00') : null;
        var to = vm.filter.to ? new Date(vm.filter.to + 'T23:59:59') : null;
        return list.filter(function (s) {
          var dt = s.sessionDateTime ? new Date(s.sessionDateTime) : null;
          if (!dt || isNaN(dt)) return true;
          if (from && dt < from) return false;
          if (to && dt > to) return false;
          return true;
        }).sort(function (a,b) {
          return new Date(a.sessionDateTime) - new Date(b.sessionDateTime);
        });
      };

      vm.load();
    }]);
})();
