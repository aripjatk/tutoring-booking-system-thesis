(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('MessageComposeController', ['apiService', 'authService', '$routeParams', '$window', '$scope', function (apiService, authService, $routeParams, $window, $scope) {
      var vm = this;
      vm.state = { busy: false, error: '' };

      var currentUser = authService.getSession();

      vm.message = {
        SenderUsername: currentUser ? currentUser.username : '',
        RecipientUsername: $routeParams.recipient || '',
        Topic: $routeParams.topic || '',
        Body: ''
      };

      vm.send = function () {
        if (!vm.message.RecipientUsername || !vm.message.Topic || !vm.message.Body) {
          vm.state.error = 'Please fill in all required fields.';
          return;
        }

        vm.state.busy = true;
        vm.state.error = '';

        var fd = new FormData();
        fd.append('SenderUsername', vm.message.SenderUsername);
        fd.append('RecipientUsername', vm.message.RecipientUsername);
        fd.append('Topic', vm.message.Topic);
        fd.append('Body', vm.message.Body);

        var fileInput = document.getElementById('messageFile');
        if (fileInput && fileInput.files[0]) {
          fd.append('File', fileInput.files[0]);
        }

        apiService.sendMessage(fd)
          .then(function () {
            $window.location.hash = '#!/messages/sent';
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? (err.data.title || err.data) : 'Failed to send message.';
            vm.state.busy = false;
          });
      };

      vm.cancel = function () {
        $window.history.back();
      };
    }]);
})();
