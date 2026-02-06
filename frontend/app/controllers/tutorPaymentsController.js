(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('TutorPaymentsController', ['apiService', 'authService', '$q', function (apiService, authService, $q) {
      var vm = this;
      vm.user = authService.getSession();

      vm.state = { busy: true, error: '', saving: false };
      vm.payments = [];
      vm.students = [];
      vm.search = '';

      vm.meansOfPayment = [
        { id: 0, label: 'Cash' },
        { id: 1, label: 'Bank transfer' },
        { id: 2, label: 'Mobile transfer (e.g. BLIK)' }
      ];

      vm.form = {};
      vm.isEditing = false;

      function resetForm() {
        vm.form = {
          studentUsername: '',
          amountPaid: 0,
          meansOfPayment: 0,
          paidOn: ''
        };
        vm.isEditing = false;
      }

      vm.load = function () {
        vm.state.busy = true; vm.state.error = '';

        var p1 = apiService.getPayments();
        var p2 = apiService.getAccounts();

        $q.all([p1, p2])
          .then(function (results) {
            var payments = results[0] || [];
            var accounts = results[1] || [];

            vm.students = accounts.filter(function (a) {
              return a && a.isTutor === false;
            });

            vm.payments = payments.filter(function(p) {
               return p.tutorUsername === vm.user.username;
            }).sort(function (a, b) {
              return new Date(b.paidOn) - new Date(a.paidOn);
            });
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to load data.';
          })
          .finally(function () { vm.state.busy = false; });
      };

      vm.startCreate = function () {
        resetForm();
        var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('paymentModal'));
        modal.show();
      };

      vm.startEdit = function (payment) {
        vm.form = angular.copy(payment);
        if (vm.form.paidOn) {
            vm.form.paidOn = new Date(vm.form.paidOn);
        }
        vm.isEditing = true;
        var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('paymentModal'));
        modal.show();
      };

      vm.save = function () {
        vm.state.saving = true; vm.state.error = '';

        var payload = angular.copy(vm.form);
        payload.tutorUsername = vm.user.username;
        if (payload.paidOn instanceof Date) {
            payload.paidOn = payload.paidOn.toISOString();
        }

        var promise;
        if (vm.isEditing) {
          promise = apiService.updatePayment(payload);
        } else {
          promise = apiService.createPayment(payload);
        }

        promise
          .then(function () {
            var modalEl = document.getElementById('paymentModal');
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            if (modal) { modal.hide(); }

            vm.load();
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to save payment.';
          })
          .finally(function () { vm.state.saving = false; });
      };

      vm.delete = function (payment) {
        if (!confirm('Are you sure you want to delete this payment record?')) return;

        vm.state.busy = true;
        apiService.deletePayment(payment.paymentRecordID)
          .then(function() {
            vm.load();
          })
          .catch(function (err) {
            vm.state.error = err && err.data ? err.data : 'Failed to delete payment.';
            vm.state.busy = false;
          });
      };

      vm.getMeansLabel = function(id) {
        var m = vm.meansOfPayment.find(x => x.id === id);
        return m ? m.label : id;
      };

      vm.load();
    }]);
})();
