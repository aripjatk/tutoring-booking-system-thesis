(function () {
  'use strict';

  angular.module('tbsApp')
    .factory('apiService', ['$http', 'APP_CONFIG', function ($http, APP_CONFIG) {

      function buildUrl(path) {
        return APP_CONFIG.apiBaseUrl + path;
      }

      function buildRootUrl(path) {
        // Removes '/api' from the end if present, then appends path
        var root = APP_CONFIG.apiBaseUrl.replace(/\/api\/?$/, '');
        return root + path;
      }

      // --- Messages ---
      function getReceivedMessages() { return $http.get(buildRootUrl('/received'), { timeout: 15000 }).then(r => r.data); }
      function getSentMessages() { return $http.get(buildRootUrl('/sent'), { timeout: 15000 }).then(r => r.data); }
      function getMessage(id) { return $http.get(buildUrl('/Message/' + id), { timeout: 15000 }).then(r => r.data); }

      function sendMessage(formData) {
        return $http.post(buildUrl('/Message'), formData, {
          timeout: 15000,
          headers: { 'Content-Type': undefined },
          transformRequest: angular.identity
        }).then(r => r.data);
      }

      function getMessageFile(id) {
        return $http.get(buildUrl('/Message/' + id + '/file'), {
          responseType: 'blob',
          timeout: 15000
        });
      }

      function getCourses() { return $http.get(buildUrl('/course'), { timeout: 15000 }).then(r => r.data); }
      function getCourse(id) { return $http.get(buildUrl('/course/' + id), { timeout: 15000 }).then(r => r.data); }
      function createCourse(course) { return $http.post(buildUrl('/course'), course, { timeout: 15000 }).then(r => r.data); }
      function updateCourse(course) { return $http.put(buildUrl('/course/' + course.courseID), course, { timeout: 15000 }); }
      function deleteCourse(id) { return $http.delete(buildUrl('/course/' + id), { timeout: 15000 }); }

      function getAccounts() { return $http.get(buildUrl('/account'), { timeout: 15000 }).then(r => r.data); }
      function registerStudent(dto) { return $http.post(buildUrl('/account/registerStudent'), dto, { timeout: 15000 }); }
      function registerTutor(dto) { return $http.post(buildUrl('/account/registerTutor'), dto, { timeout: 15000 }); }
      function deactivateAccount(username) { return $http.post(buildUrl('/account/deactivate/' + encodeURIComponent(username)), null, { timeout: 15000 }); }

      function getStudentsByCourse(courseId) { return $http.get(buildUrl('/studentcourse/' + courseId), { timeout: 15000 }).then(r => r.data); }
      function assignStudentToCourse(studentUsername, courseId, frequency, endDateIso) {
        return $http.post(buildUrl('/studentcourse'), {
          studentUsername: studentUsername,
          courseID: courseId,
          frequency: frequency,
          endDate: endDateIso
        }, { timeout: 15000 }).then(r => r.data);
      }
      function updateStudentCourse(studentUsername, courseId, frequency, endDateIso) {
        return $http.put(buildUrl('/studentcourse/' + courseId + '/' + encodeURIComponent(studentUsername)), {
          studentUsername: studentUsername,
          courseID: courseId,
          frequency: frequency,
          endDate: endDateIso
        }, { timeout: 15000 });
      }

      function getSessions() { return $http.get(buildUrl('/session'), { timeout: 15000 }).then(r => r.data); }
      function getSession(id) { return $http.get(buildUrl('/session/' + id), { timeout: 15000 }).then(r => r.data); }
      function createSession(session) { return $http.post(buildUrl('/session'), session, { timeout: 15000 }).then(r => r.data); }
      function updateSession(session) { return $http.put(buildUrl('/session/' + session.sessionID), session, { timeout: 15000 }); }
      function deleteSession(id) { return $http.delete(buildUrl('/session/' + id), { timeout: 15000 }); }
      function acceptSession(id) { return $http.get(buildUrl('/session/' + id + '/accept'), { timeout: 15000 }); }
      function rejectSession(id) { return $http.get(buildUrl('/session/' + id + '/reject'), { timeout: 15000 }); }

      function getMyCourses() { return $http.get(buildUrl('/studentcourse'), { timeout: 15000 }).then(r => r.data); }
      function getMySessions() { return $http.get(buildUrl('/session'), { timeout: 15000 }).then(r => r.data); }

      function getHomeworkAssignments() { return $http.get(buildUrl('/HomeworkAssignment'), { timeout: 15000 }).then(r => r.data); }
      function getHomeworkAssignment(id) { return $http.get(buildUrl('/HomeworkAssignment/' + id), { timeout: 15000 }).then(r => r.data); }

      function createHomeworkAssignment(formData) {
        return $http.post(buildUrl('/HomeworkAssignment'), formData, {
          timeout: 15000,
          headers: { 'Content-Type': undefined },
          transformRequest: angular.identity
        }).then(r => r.data);
      }

      function updateHomeworkAssignment(id, data) { return $http.put(buildUrl('/HomeworkAssignment/' + id), data, { timeout: 15000 }); }

      function uploadHomeworkSolution(id, file) {
        var fd = new FormData();
        fd.append('file', file);
        return $http.post(buildUrl('/HomeworkAssignment/' + id + '/file'), fd, {
          timeout: 15000,
          headers: { 'Content-Type': undefined },
          transformRequest: angular.identity
        });
      }

      function downloadHomeworkFile(id) {
        return $http.get(buildUrl('/HomeworkAssignment/' + id + '/file'), {
          responseType: 'blob',
          timeout: 15000
        });
      }

      function getNotes() { return $http.get(buildUrl('/Note'), { timeout: 15000 }).then(r => r.data); }
      function getNote(id) { return $http.get(buildUrl('/Note/' + id), { timeout: 15000 }).then(r => r.data); }
      function createNote(note) { return $http.post(buildUrl('/Note'), note, { timeout: 15000 }).then(r => r.data); }
      function updateNote(note) { return $http.put(buildUrl('/Note/' + note.noteID), note, { timeout: 15000 }); }
      function deleteNote(id) { return $http.delete(buildUrl('/Note/' + id), { timeout: 15000 }); }

      return {
        getCourses, getCourse, createCourse, updateCourse, deleteCourse,
        getAccounts, registerStudent, registerTutor, deactivateAccount,
        getStudentsByCourse, assignStudentToCourse, updateStudentCourse,
        getSessions, getSession, createSession, updateSession, deleteSession, acceptSession, rejectSession,
        getMyCourses, getMySessions,
        getHomeworkAssignments, getHomeworkAssignment, createHomeworkAssignment, updateHomeworkAssignment, uploadHomeworkSolution, downloadHomeworkFile,
        getNotes, getNote, createNote, updateNote, deleteNote,
        getReceivedMessages, getSentMessages, getMessage, sendMessage, getMessageFile,
        getTeachingMaterials, createTeachingMaterial, deleteTeachingMaterial, getTeachingMaterialFile,
        getNotifications, deleteNotification,
        getPayments, createPayment, updatePayment, deletePayment
      };

      function getPayments() { return $http.get(buildUrl('/PaymentRecord'), { timeout: 15000 }).then(r => r.data); }
      function createPayment(payment) { return $http.post(buildUrl('/PaymentRecord'), payment, { timeout: 15000 }).then(r => r.data); }
      function updatePayment(payment) { return $http.put(buildUrl('/PaymentRecord/' + payment.paymentRecordID), payment, { timeout: 15000 }); }
      function deletePayment(id) { return $http.delete(buildUrl('/PaymentRecord/' + id), { timeout: 15000 }); }

      function getNotifications() { return $http.get(buildUrl('/Notification'), { timeout: 15000 }).then(r => r.data); }
      function deleteNotification(id) { return $http.delete(buildUrl('/Notification/' + id), { timeout: 15000 }); }

      function getTeachingMaterials() { return $http.get(buildUrl('/TeachingMaterial'), { timeout: 15000 }).then(r => r.data); }

      function createTeachingMaterial(formData) {
        return $http.post(buildUrl('/TeachingMaterial'), formData, {
          timeout: 15000,
          headers: { 'Content-Type': undefined },
          transformRequest: angular.identity
        }).then(r => r.data);
      }

      function deleteTeachingMaterial(id) { return $http.delete(buildUrl('/TeachingMaterial/' + id), { timeout: 15000 }); }

      function getTeachingMaterialFile(id) {
        return $http.get(buildUrl('/TeachingMaterial/' + id + '/file'), {
          responseType: 'blob',
          timeout: 15000
        });
      }
    }]);
})();
