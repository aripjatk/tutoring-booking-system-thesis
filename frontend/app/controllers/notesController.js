(function () {
  'use strict';

  angular.module('tbsApp')
    .controller('NotesController', ['apiService', 'authService', function (apiService, authService) {
      var vm = this;
      vm.user = authService.getSession();
      vm.state = { busy: false, error: '' };
      vm.notes = [];
      vm.currentNote = {};
      vm.isFormVisible = false;
      vm.isEditing = false;

      vm.refresh = function () {
        vm.state.busy = true;
        vm.state.error = '';
        apiService.getNotes()
          .then(function (data) {
            // Sort by date descending
            vm.notes = (data || []).sort(function(a, b) {
              return new Date(b.date) - new Date(a.date);
            });
          })
          .catch(function (err) {
            vm.state.error = 'Failed to load notes.';
          })
          .finally(function () {
            vm.state.busy = false;
          });
      };

      vm.initNewNote = function () {
        vm.currentNote = {
          accountUsername: vm.user.username,
          date: new Date().toISOString(),
          body: ''
        };
        vm.isEditing = false;
        vm.isFormVisible = true;
      };

      vm.editNote = function (note) {
        vm.currentNote = angular.copy(note);
        vm.isEditing = true;
        vm.isFormVisible = true;
      };

      vm.cancel = function () {
        vm.currentNote = {};
        vm.isFormVisible = false;
        vm.isEditing = false;
      };

      vm.saveNote = function () {
        if (!vm.currentNote.body) return;

        vm.state.busy = true;
        vm.state.error = '';

        if (vm.isEditing) {
          apiService.updateNote(vm.currentNote)
            .then(function () {
              vm.refresh();
              vm.cancel();
            })
            .catch(function (err) {
              vm.state.error = 'Failed to update note.';
              vm.state.busy = false;
            });
        } else {
          if (!vm.currentNote.date) vm.currentNote.date = new Date().toISOString();

          apiService.createNote(vm.currentNote)
            .then(function () {
              vm.refresh();
              vm.cancel();
            })
            .catch(function (err) {
              vm.state.error = 'Failed to create note.';
              vm.state.busy = false;
            });
        }
      };

      vm.deleteNote = function (id) {
        if (!confirm('Are you sure you want to delete this note?')) return;

        vm.state.busy = true;
        apiService.deleteNote(id)
          .then(function () {
            vm.refresh();
          })
          .catch(function (err) {
            vm.state.error = 'Failed to delete note.';
            vm.state.busy = false;
          });
      };

      vm.refresh();
    }]);
})();
