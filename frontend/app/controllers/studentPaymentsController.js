(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('StudentPaymentsController', ['apiService', 'authService', function (apiService, authService) {
      var vm = this;
      vm.user = authService.getSession();

      vm.state = { busy: true, error: '' };
      vm.payments = [];
      vm.search = '';

      vm.meansOfPayment = [
        { id: 0, label: 'Cash' },
        { id: 1, label: 'Bank transfer' },
        { id: 2, label: 'Mobile transfer (e.g. BLIK)' }
      ];

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';
        apiService.getPayments()
          .then(function (data) {
            vm.payments = (data || []).filter(function(p) {
               return p.studentUsername === vm.user.username;
            }).sort(function (a, b) {
              return new Date(b.paidOn) - new Date(a.paidOn);
            });
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to load payments.';
          })
          .finally(function () { vm.state.busy = false; });
      };

      vm.getMeansLabel = function(id) {
        var m = vm.meansOfPayment.find(x => x.id === id);
        return m ? m.label : id;
      };

      vm.load();
    }]);
})();
