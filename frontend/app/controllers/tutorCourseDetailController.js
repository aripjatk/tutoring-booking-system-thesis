(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('TutorCourseDetailController', ['apiService', 'authService', '$routeParams', '$q', '$window',
      function (apiService, authService, $routeParams, $q, $window) {

      var vm = this;
      vm.user = authService.getSession();
      vm.courseId = parseInt($routeParams.id, 10);

      vm.state = { busy: true, error: '', saving: false, enrolling: false };
      vm.course = null;

      vm.enrollments = []; // Student_Course
      vm.accounts = [];    // all accounts for pick-list
      vm.students = [];

      vm.enrollForm = {
        studentUsername: '',
        frequency: 'weekly',
        endDate: ''
      };

      vm.teachingMaterials = [];
      vm.newMaterial = { name: '' };
      vm.state.uploading = false;

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        $q.all([
          apiService.getCourse(vm.courseId),
          apiService.getStudentsByCourse(vm.courseId),
          apiService.getAccounts(),
          apiService.getTeachingMaterials()
        ]).then(function (values) {
          vm.course = values[0];
          vm.enrollments = values[1] || [];
          vm.accounts = values[2] || [];
          vm.students = vm.accounts.filter(a => a && a.isTutor === false);

          var allMaterials = values[3] || [];
          vm.teachingMaterials = allMaterials.filter(m => m.courseID === vm.courseId);

          // default end date: +3 months
          if (!vm.enrollForm.endDate) {
            var d = new Date();
            d.setMonth(d.getMonth() + 3);
            vm.enrollForm.endDate = d;
          }

        }).catch(function (err) {
          vm.state.error = (err && err.data) ? err.data : 'Failed to load course details.';
        }).finally(function () { vm.state.busy = false; });
      };

      vm.uploadMaterial = function () {
        var fileInput = document.getElementById('materialFile');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
          alert('Please select a file.');
          return;
        }
        if (!vm.newMaterial.name) {
          alert('Please enter a name for the material.');
          return;
        }

        var fd = new FormData();
        fd.append('Name', vm.newMaterial.name);
        fd.append('CourseID', vm.courseId);
        fd.append('File', fileInput.files[0]);

        vm.state.uploading = true; vm.state.error = '';
        apiService.createTeachingMaterial(fd)
          .then(function () {
            vm.newMaterial.name = '';
            fileInput.value = '';
            vm.load();
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to upload material.';
          })
          .finally(function () { vm.state.uploading = false; });
      };

      vm.deleteMaterial = function (id) {
        if (!confirm('Are you sure you want to delete this material?')) return;
        vm.state.busy = true;
        apiService.deleteTeachingMaterial(id)
          .then(function () { vm.load(); })
          .catch(function (err) {
             vm.state.error = err && err.data ? err.data : 'Failed to delete material.';
             vm.state.busy = false;
          });
      };

      vm.save = function () {
        vm.state.saving = true; vm.state.error = '';
        apiService.updateCourse(vm.course)
          .then(function () { alert('Saved.'); })
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to save course.'; })
          .finally(function () { vm.state.saving = false; });
      };

      vm.enroll = function () {
        vm.state.enrolling = true; vm.state.error = '';

        try {
          var dateVal = vm.enrollForm.endDate;
          var dateStr = '';
          if (dateVal instanceof Date) {
            var year = dateVal.getFullYear();
            var month = ('0' + (dateVal.getMonth() + 1)).slice(-2);
            var day = ('0' + dateVal.getDate()).slice(-2);
            dateStr = year + '-' + month + '-' + day;
          } else {
            dateStr = dateVal;
          }
          var endIso = new Date(dateStr + 'T00:00:00Z').toISOString();

          apiService.assignStudentToCourse(vm.enrollForm.studentUsername, vm.courseId, vm.enrollForm.frequency, endIso)
            .then(function () { vm.enrollForm.studentUsername = ''; vm.load(); })
            .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to assign student.'; })
            .finally(function () { vm.state.enrolling = false; });
        } catch (ex) {
          vm.state.error = 'Invalid date selected.';
          vm.state.enrolling = false;
        }
      };

      vm.updateEnrollment = function (e) {
        var endIso = new Date(e.endDate).toISOString();
        apiService.updateStudentCourse(e.studentUsername, e.courseID, e.frequency, endIso)
          .then(function () { alert('Enrollment updated.'); })
          .catch(function (err) { vm.state.error = err && err.data ? err.data : 'Failed to update enrollment.'; });
      };

      vm.back = function () { $window.location.hash = '#!/tutor/courses'; };

      vm.load();
    }]);
})();
