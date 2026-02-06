(function () {
  'use strict';

  angular.module('tbsApp')
    .factory('authService', ['$http', '$q', 'APP_CONFIG', function ($http, $q, APP_CONFIG) {

      var STORAGE_KEY = 'tbs_session_full';
      var session = loadSession();

      function loadSession() {
        try { var raw = localStorage.getItem(STORAGE_KEY); return raw ? JSON.parse(raw) : null; }
        catch (e) { return null; }
      }

      function saveSession(s) {
        session = s;
        if (s) localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
        else localStorage.removeItem(STORAGE_KEY);
      }

      function login(username, password) {
        return $http.post(APP_CONFIG.apiBaseUrl + '/account/login', { username: username, password: password }, { timeout: 15000 })
          .then(function (res) {
            var dto = res.data || {};
            if (!dto.token || !dto.username) throw new Error('Invalid login response');

            return $http.get(APP_CONFIG.apiBaseUrl + '/account/' + encodeURIComponent(dto.username), {
              headers: { Authorization: 'Bearer ' + dto.token },
              timeout: 15000
            }).then(function (accRes) {
              var acc = accRes.data || {};
              var newSession = {
                username: dto.username,
                token: dto.token,
                isTutor: !!acc.isTutor,
                fetchedAt: new Date().toISOString()
              };
              saveSession(newSession);
              return newSession;
            });
          });
      }

      function logout() { saveSession(null); }

      function getSession() { return session; }
      function isAuthenticated() { return !!(session && session.token); }
      function isTutor() { return !!(session && session.isTutor); }
      function isStudent() { return isAuthenticated() && !isTutor(); }

      function getToken() { return session ? session.token : null; }
      function clearSession() { saveSession(null); }

      function requireAuth(role) {
        var d = $q.defer();
        if (!isAuthenticated()) {
          d.reject('not_authenticated');
          window.location.hash = '#!/login';
          return d.promise;
        }
        if (role === 'tutor' && !isTutor()) {
          d.reject('not_authorized');
          alert('This view is available to Tutor accounts only.');
          window.location.hash = '#!/student/dashboard';
          return d.promise;
        }
        if (role === 'student' && !isStudent()) {
          d.reject('not_authorized');
          alert('This view is available to Student accounts only.');
          window.location.hash = '#!/tutor/dashboard';
          return d.promise;
        }
        d.resolve(true);
        return d.promise;
      }

      return {
        login: login, logout: logout,
        getSession: getSession,
        isAuthenticated: isAuthenticated,
        isTutor: isTutor,
        isStudent: isStudent,
        getToken: getToken,
        clearSession: clearSession,
        requireAuth: requireAuth
      };
    }]);
})();
