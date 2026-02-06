(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('MessageDetailController', ['apiService', '$routeParams', '$window', function (apiService, $routeParams, $window) {
      var vm = this;
      vm.state = { busy: true, error: '' };
      vm.message = {};

      vm.load = function () {
        vm.state.busy = true;
        apiService.getMessage($routeParams.id)
          .then(function (data) {
            vm.message = data;
          })
          .catch(function (err) {
            vm.state.error = 'Failed to load message.';
          })
          .finally(function () {
            vm.state.busy = false;
          });
      };

      vm.downloadAttachment = function () {
        if (!vm.message.attachmentFileName) return;

        apiService.getMessageFile(vm.message.messageID)
          .then(function (response) {
            var blob = new Blob([response.data]);
            var link = document.createElement('a');
            link.href = window.URL.createObjectURL(blob);
            link.download = vm.message.attachmentFileName;
            link.click();
            window.URL.revokeObjectURL(link.href);
          })
          .catch(function () {
            alert('Failed to download file.');
          });
      };

      vm.back = function () {
        $window.history.back();
      };

      vm.reply = function () {
         $window.location.hash = '#!/messages/new?recipient=' + encodeURIComponent(vm.message.senderUsername) + '&topic=Re: ' + encodeURIComponent(vm.message.topic);
      };

      vm.load();
    }]);
})();
