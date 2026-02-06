(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('TutorSessionDetailController', ['apiService', '$routeParams', '$window', function (apiService, $routeParams, $window) {
      var vm = this;
      vm.sessionId = parseInt($routeParams.id, 10);
      vm.state = { busy: true, error: '', success: '' };
      vm.session = null;
      vm.newAssignment = { name: '', objective: '', file: null };

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        apiService.getSession(vm.sessionId)
          .then(function (s) {
              if (s.sessionDateTime && !s.sessionDateTime.endsWith('Z')) s.sessionDateTime += 'Z';
              vm.session = s;
              vm.session._dt = s.sessionDateTime ? new Date(s.sessionDateTime) : null;
              if (!vm.session.homeworkAssignments) vm.session.homeworkAssignments = [];

              if (vm.session.courseID) {
                  return apiService.getCourse(vm.session.courseID);
              }
          })
          .then(function (course) {
              if (vm.session && course) {
                  vm.session.course = course;
              }
          })
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to load session.'; })
          .finally(function () { vm.state.busy = false; });
      };

      vm.getConfirmationStatusText = function () {
          if (!vm.session) return '';
          switch (vm.session.confirmationStatus) {
              case 1: return 'Confirmed';
              case 2: return 'Rejected';
              default: return 'Unknown';
          }
      };

      vm.updatePaidStatus = function () {
          vm.state.updatingPaid = true;
          apiService.updateSession(vm.session)
              .catch(function (err) {
                  vm.state.error = err && err.data ? err.data : 'Failed to update payment status.';
                  vm.session.isPaidFor = !vm.session.isPaidFor; // Revert
              })
              .finally(function () { vm.state.updatingPaid = false; });
      };

      vm.openRescheduleModal = function () {
          vm.rescheduleDate = vm.session._dt ? new Date(vm.session._dt.getTime()) : new Date();
          var el = document.getElementById('rescheduleModal');
          var modal = bootstrap.Modal.getOrCreateInstance(el);
          modal.show();
      };

      vm.saveReschedule = function () {
          if (!vm.rescheduleDate) return;

          var originalDate = vm.session.sessionDateTime;
          var originalDt = vm.session._dt;

          vm.session.sessionDateTime = vm.rescheduleDate.toISOString();
          vm.session._dt = vm.rescheduleDate;

          vm.state.busy = true;
          apiService.updateSession(vm.session)
              .then(function () {
                  vm.state.success = 'Session rescheduled.';
                  var el = document.getElementById('rescheduleModal');
                  var modal = bootstrap.Modal.getInstance(el);
                  if (modal) modal.hide();
              })
              .catch(function (err) {
                  vm.state.error = err && err.data ? err.data : 'Failed to reschedule.';
                  vm.session.sessionDateTime = originalDate;
                  vm.session._dt = originalDt;
              })
              .finally(function () {
                  vm.state.busy = false;
              });
      };

      vm.back = function () { $window.location.hash = '#!/tutor/sessions'; };

      vm.delete = function () {
        if (!confirm('Delete this session?')) return;
        apiService.deleteSession(vm.sessionId)
          .then(function () { $window.location.hash = '#!/tutor/sessions'; })
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to delete.'; });
      };

      vm.createAssignment = function () {
          if (!vm.newAssignment.name) { alert('Name is required'); return; }

          vm.state.busy = true; vm.state.error = ''; vm.state.success = '';

          var fd = new FormData();
          fd.append('SessionID', vm.sessionId);
          fd.append('Name', vm.newAssignment.name);
          fd.append('Objective', vm.newAssignment.objective || '');

          var fileInput = document.getElementById('newAssignmentFile');
          if (fileInput && fileInput.files.length > 0) {
              fd.append('File', fileInput.files[0]);
          }

          apiService.createHomeworkAssignment(fd)
            .then(function (hw) {
                vm.state.success = 'Assignment created.';
                vm.newAssignment = { name: '', objective: '', file: null };
                if (fileInput) fileInput.value = '';
                vm.load();
            })
            .catch(function (err) {
                vm.state.error = err && err.data ? err.data : 'Failed to create assignment.';
                vm.state.busy = false;
            });
      };

      vm.saveFeedback = function (hw) {
          if (!hw.solutionFeedback) return;
          vm.state.busy = true; vm.state.success = '';

          apiService.updateHomeworkAssignment(hw.homeworkAssignmentID, hw)
            .then(function () {
                vm.state.success = 'Feedback saved.';
            })
            .catch(function (err) {
                vm.state.error = err && err.data ? err.data : 'Failed to save feedback.';
            })
            .finally(function () { vm.state.busy = false; });
      };

      vm.downloadFile = function (hw) {
         if (!hw.solutionFileName) return;
         apiService.downloadHomeworkFile(hw.homeworkAssignmentID)
           .then(function (response) {
               var blob = response.data;
               var link = document.createElement('a');
               link.href = window.URL.createObjectURL(blob);
               link.download = hw.solutionFileName;
               link.click();
               window.URL.revokeObjectURL(link.href);
           })
           .catch(function () { alert('Failed to download file.'); });
      };

      vm.load();
    }]);
})();
