(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('TutorSessionsController', ['apiService', 'authService', '$q', '$window', function (apiService, authService, $q, $window) {
      var vm = this;
      vm.user = authService.getSession();
      vm.state = { busy: true, error: '', saving: false };

      vm.sessions = [];
      vm.courses = [];
      vm.students = [];

      vm.filter = { from: '', to: '' };

      vm.createForm = {
        courseID: null,
        studentUsername: '',
        sessionDateTimeLocal: '',
        isPaidFor: false,
        confirmationStatus: 0 // Unknown
      };

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        $q.all([apiService.getSessions(), apiService.getCourses(), apiService.getAccounts()])
          .then(function (values) {
            vm.sessions = values[0] || [];
            vm.courses = (values[1] || []).filter(c => !c.tutorUsername || c.tutorUsername === vm.user.username);
            vm.students = (values[2] || []).filter(a => a && a.isTutor === false);

            vm.sessions.forEach(function (s) {
              if (s.sessionDateTime && !s.sessionDateTime.endsWith('Z')) s.sessionDateTime += 'Z';
              var match = vm.courses.find(function (c) { return c.courseID === s.courseID; });
              if (match) s.course = match;
            });

            if (!vm.createForm.courseID && vm.courses.length) vm.createForm.courseID = vm.courses[0].courseID;
          })
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to load sessions.'; })
          .finally(function () { vm.state.busy = false; });
      };

      vm.getStatusLabel = function (status) {
        switch (status) {
          case 0: return 'Unknown';
          case 1: return 'Confirmed';
          case 2: return 'Rejected';
          default: return status;
        }
      };

      vm.openDetail = function (id) { $window.location.hash = '#!/tutor/sessions/' + id; };

      vm.filteredSessions = function () {
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

      vm.create = function () {
        vm.state.saving = true; vm.state.error = '';
        try {
          var dt = new Date(vm.createForm.sessionDateTimeLocal);
          if (isNaN(dt)) throw new Error('Invalid date/time');
          var payload = {
            studentUsername: vm.createForm.studentUsername,
            courseID: vm.createForm.courseID,
            sessionDateTime: dt.toISOString(),
            isPaidFor: !!vm.createForm.isPaidFor,
            confirmationStatus: vm.createForm.confirmationStatus
          };
          apiService.createSession(payload)
            .then(function () {
              vm.createForm.studentUsername = '';
              vm.createForm.sessionDateTimeLocal = '';
              vm.createForm.isPaidFor = false;
              vm.createForm.confirmationStatus = 0;
              vm.load();
            })
            .catch(function (err) {
              vm.state.error = err && err.data ? err.data : 'Failed to create session.';
            })
            .finally(function () { vm.state.saving = false; });
        } catch (e) {
          vm.state.error = e.message || 'Failed to create session.';
          vm.state.saving = false;
        }
      };

      vm.delete = function (s) {
        if (!confirm('Delete session #' + s.sessionID + '?')) return;
        apiService.deleteSession(s.sessionID)
          .then(vm.load)
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to delete session.'; });
      };

      vm.load();
    }]);
})();
