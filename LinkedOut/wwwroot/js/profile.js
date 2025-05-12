// profile.js - Handle profile settings functionality

$(document).ready(function() {
    console.log('Profile JS initialized');
    
    // Add debugging helper to monitor all AJAX calls
    $(document).ajaxSend(function(event, jqxhr, settings) {
        console.log('AJAX request started:', settings.url, settings.type, settings.data);
    });
    
    $(document).ajaxComplete(function(event, jqxhr, settings) {
        console.log('AJAX request completed:', settings.url, 'Status:', jqxhr.status);
        try {
            if (jqxhr.responseJSON) {
                console.log('Response data:', jqxhr.responseJSON);
            } else if (jqxhr.responseText) {
                console.log('Response text:', jqxhr.responseText.substring(0, 500)); // First 500 chars
            }
        } catch (e) {
            console.error('Error parsing AJAX response:', e);
        }
    });
    
    // Profile image upload
    $('#profile-image').on('change', function(e) {
        console.log('Profile image change detected');
        const file = e.target.files[0];
        if (!file) {
            console.log('No file selected');
            return;
        }
        
        if (!file.type.match('image.*')) {
            toastr.error('Please select an image file');
            console.log('Invalid file type:', file.type);
            return;
        }
        
        console.log('Uploading profile image:', file.name, 'Size:', file.size);
        
        const formData = new FormData();
        formData.append('profileImage', file);
        
        // Show loading indicator
        const $profileImg = $('.avatar-profile img');
        const currentSrc = $profileImg.attr('src');
        $profileImg.css('opacity', '0.5');
        
        // Display upload in progress notification
        toastr.info('Uploading profile image...');
        
        $.ajax({
            url: '/Profile/UpdateProfileImage',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function(response) {
                console.log('Profile image upload response:', response);
                if (response.success) {
                    // Update the profile image with the new one - get URL from data property
                    const imageUrl = response.data?.imageUrl;
                    if (imageUrl) {
                        const newImageUrl = imageUrl + '?v=' + new Date().getTime();
                        console.log('Setting new image URL:', newImageUrl);
                        $profileImg.attr('src', newImageUrl);
                        toastr.success(response.message || 'Profile image updated successfully');
                        
                        // Force a reload of the image to ensure it's shown
                        $profileImg.on('error', function() {
                            console.log('Error loading image, trying again...');
                            $(this).attr('src', currentSrc);
                            toastr.warning('Image uploaded but not displayed. Try refreshing the page.');
                        });
                    } else {
                        console.error('No image URL received in response');
                        toastr.warning('Image uploaded but URL not received. Try refreshing the page.');
                    }
                } else {
                    console.error('Failed to update profile image:', response.message);
                    toastr.error(response.message || 'Failed to update profile image');
                    $profileImg.attr('src', currentSrc);
                }
                $profileImg.css('opacity', '1');
            },
            error: function(xhr, status, error) {
                console.error('Profile image upload error:', error);
                console.error('Status:', status);
                console.error('Response:', xhr.responseText);
                toastr.error('An error occurred while updating profile image');
                $profileImg.attr('src', currentSrc);
                $profileImg.css('opacity', '1');
            }
        });
    });
    
    // Cover image upload
    $('#upload-cover-image').on('change', function(e) {
        console.log('Cover image change detected');
        const file = e.target.files[0];
        if (!file) {
            console.log('No file selected');
            return;
        }
        
        if (!file.type.match('image.*')) {
            toastr.error('Please select an image file');
            console.log('Invalid file type:', file.type);
            return;
        }
        
        console.log('Uploading cover image:', file.name, 'Size:', file.size);
        
        const formData = new FormData();
        formData.append('coverImage', file);
        
        // Show loading indicator
        const $coverImg = $('.bg-holder');
        const currentBg = $coverImg.css('background-image');
        $coverImg.css('opacity', '0.5');
        
        // Display upload in progress notification
        toastr.info('Uploading cover image...');
        
        $.ajax({
            url: '/Profile/UpdateCoverImage',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function(response) {
                console.log('Cover image upload response:', response);
                if (response.success) {
                    // Update the cover image with the new one
                    try {
                        const imageUrl = response.data?.imageUrl;
                        if (imageUrl) {
                            const newImageUrl = 'url(' + imageUrl + '?v=' + new Date().getTime() + ')';
                            console.log('Setting new cover image:', newImageUrl);
                            $coverImg.css('background-image', newImageUrl);
                            toastr.success(response.message || 'Cover image updated successfully');
                            
                            // Load the image in the background to check if it's accessible
                            const testImg = new Image();
                            testImg.onload = function() {
                                console.log('Cover image loaded successfully');
                            };
                            testImg.onerror = function() {
                                console.error('Cover image failed to load');
                                toastr.warning('Image uploaded but not displayed. Try refreshing the page.');
                            };
                            testImg.src = imageUrl + '?v=' + new Date().getTime();
                        } else {
                            console.error('No image URL received in response');
                            toastr.warning('Cover image uploaded but URL not received. Try refreshing the page.');
                        }
                    } catch (err) {
                        console.error('Error updating cover image display:', err);
                        // Still show success since upload worked, but display had issues
                        toastr.warning('Cover image uploaded successfully. Refresh the page to see changes.');
                    }
                } else {
                    console.error('Failed to update cover image:', response.message);
                    toastr.error(response.message || 'Failed to update cover image');
                    try {
                        $coverImg.css('background-image', currentBg);
                    } catch (err) {
                        console.error('Error resetting cover image:', err);
                    }
                }
                $coverImg.css('opacity', '1');
            },
            error: function(xhr, status, error) {
                console.error('Cover image update error:', error);
                console.error('Status:', status);
                console.error('Response:', xhr.responseText);
                toastr.error('An error occurred while updating cover image');
                try {
                    $coverImg.css('background-image', currentBg);
                } catch (err) {
                    console.error('Error resetting cover image:', err);
                }
                $coverImg.css('opacity', '1');
            }
        });
    });
    
    // Profile update form
    $('#profile-form').on('submit', function(e) {
        // Explicitly prevent the default form submission
        e.preventDefault();
        
        console.log('Profile form submitted');
        
        const formData = {
            firstName: $('#first-name').val(),
            lastName: $('#last-name').val(),
            email: $('#email1').val(),
            phone: $('#email2').val(),
            headline: $('#email3').val(),
            about: $('#intro').val()
        };
        
        console.log('Profile update data:', formData);
        
        // Show loading
        const $submitBtn = $(this).find('button[type="submit"]');
        const originalText = $submitBtn.html();
        $submitBtn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Updating...').prop('disabled', true);
        
        // Cache form values in case of error
        const cachedFormValues = {
            firstName: formData.firstName,
            lastName: formData.lastName,
            email: formData.email,
            phone: formData.phone,
            headline: formData.headline,
            about: formData.about
        };
        
        // Make the AJAX request with explicit complete callback
        $.ajax({
            url: '/Profile/UpdateProfile',
            type: 'POST',
            data: formData,
            dataType: 'json',
            success: function(response) {
                console.log('Profile update response:', response);
                if (response && response.success) {
                    toastr.success(response.message || 'Profile updated successfully');
                    
                    // Highlight the updated fields to show success
                    $('#first-name, #last-name, #email1, #email2, #email3, #intro').addClass('border-success');
                    setTimeout(function() {
                        $('#first-name, #last-name, #email1, #email2, #email3, #intro').removeClass('border-success');
                    }, 2000);
                } else {
                    console.error('Failed to update profile:', response?.message);
                    toastr.error(response?.message || 'Failed to update profile');
                    
                    // Highlight the fields in error
                    $('#first-name, #last-name, #email1, #email2, #email3, #intro').addClass('border-danger');
                    setTimeout(function() {
                        $('#first-name, #last-name, #email1, #email2, #email3, #intro').removeClass('border-danger');
                    }, 2000);
                }
                $submitBtn.html(originalText).prop('disabled', false);
            },
            error: function(xhr, status, error) {
                console.error('Profile update error:', error);
                console.error('Status:', status);
                console.error('Response:', xhr.responseText);
                toastr.error('An error occurred while updating profile');
                
                // Try to parse response if it's JSON
                try {
                    const errorResponse = JSON.parse(xhr.responseText);
                    if (errorResponse.message) {
                        toastr.error(errorResponse.message);
                    }
                } catch (e) {
                    // Not JSON or parsing failed
                }
                
                // Restore cached values if the form was cleared
                $('#first-name').val(cachedFormValues.firstName);
                $('#last-name').val(cachedFormValues.lastName);
                $('#email1').val(cachedFormValues.email);
                $('#email2').val(cachedFormValues.phone);
                $('#email3').val(cachedFormValues.headline);
                $('#intro').val(cachedFormValues.about);
                
                // Highlight the fields in error
                $('#first-name, #last-name, #email1, #email2, #email3, #intro').addClass('border-danger');
                setTimeout(function() {
                    $('#first-name, #last-name, #email1, #email2, #email3, #intro').removeClass('border-danger');
                }, 2000);
                
                $submitBtn.html(originalText).prop('disabled', false);
            },
            complete: function() {
                console.log('Profile update request completed');
                $submitBtn.html(originalText).prop('disabled', false);
            }
        });
        
        // Make extra sure we're not submitting the form
        return false;
    });
    
    // Add direct click handler on the submit button as a fallback
    $('#profile-form button[type="submit"]').on('click', function(e) {
        e.preventDefault();
        
        // Only manually trigger form submission if not already in progress
        if (!$(this).prop('disabled')) {
            $('#profile-form').submit();
        }
        
        return false;
    });
}); 