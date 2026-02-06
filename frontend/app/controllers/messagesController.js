(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('MessagesController', ['apiService', '$window', '$location', function (apiService, $window, $location) {
      var vm = this;
      vm.state = { busy: false, error: '' };
      vm.activeTab = $location.path().indexOf('/sent') !== -1 ? 'sent' : 'inbox';
      vm.messages = [];
      vm.search = '';

      vm.setTab = function (tab) {
        if (tab === 'sent') $window.location.hash = '#!/messages/sent';
        else $window.location.hash = '#!/messages';
      };

      vm.load = function () {
        vm.state.busy = true;
        vm.state.error = '';
        vm.messages = [];

        var promise;
        if (vm.activeTab === 'sent') {
          promise = apiService.getSentMessages();
        } else {
          promise = apiService.getReceivedMessages();
        }

        promise
          .then(function (data) {
            vm.messages = (data || []).sort(function (a, b) {
              return new Date(b.sentOn) - new Date(a.sentOn);
            });
          })
          .catch(function (err) {
            vm.state.error = 'Failed to load messages.';
          })
          .finally(function () {
            vm.state.busy = false;
          });
      };

      vm.viewMessage = function (id) {
        $window.location.hash = '#!/messages/' + id;
      };

      vm.compose = function () {
        $window.location.hash = '#!/messages/new';
      };

      // Initial load
      vm.load();
    }]);
})();
