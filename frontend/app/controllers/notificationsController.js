(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('NotificationsController', ['apiService', 'authService', '$window', '$rootScope', function (apiService, authService, $window, $rootScope) {
      var vm = this;

      if (!authService.isAuthenticated()) {
        $window.location.hash = '#!/login';
        return;
      }

      vm.state = { busy: true, error: '' };
      vm.notifications = [];

      m.typeLabels = {
        0: 'Session accepted',
        1: 'Session rejected',
        2: 'Homework solution uploaded',
        3: 'Message received',
        4: 'Homework assigned',
        5: 'Session created'
      };

      vm.getTypeLabel = function (typeId) {
        return vm.typeLabels[typeId] || 'Notification';
      };

      vm.load = function () {
        vm.state.busy = true;
        vm.state.error = '';
        apiService.getNotifications()
          .then(function (data) {
            vm.notifications = data || [];
            vm.notifications.sort(function (a, b) {
              return new Date(b.notificationTime) - new Date(a.notificationTime);
            });
          })
          .catch(function (err) {
            vm.state.error = 'Failed to load notifications.';
          })
          .finally(function () {
            vm.state.busy = false;
          });
      };

      vm.delete = function (id) {
        if (vm.state.busy) return;
        vm.state.busy = true;

        apiService.deleteNotification(id)
          .then(function () {
            // Remove from local list
            vm.notifications = vm.notifications.filter(function (n) {
              return n.notificationID !== id;
            });
            $rootScope.$broadcast('notificationsUpdated');
          })
          .catch(function (err) {
            alert('Failed to delete notification.');
          })
          .finally(function () {
            vm.state.busy = false;
          });
      };

      vm.load();
    }]);
})();
